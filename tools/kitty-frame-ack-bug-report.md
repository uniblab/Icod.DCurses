# Kitty animation-frame ACK bug report

**Status: unresolved.** Submission to `kovidgoyal/kitty` was attempted on
2026-10-04, but GitHub returned HTTP 403, `Resource not accessible by integration`.
No upstream issue was created. The report below is ready for manual submission
at https://github.com/kovidgoyal/kitty/issues/new?template=bug_report.md.
No production workaround has been implemented; further investigation is deferred
at the maintainer's request.

---

## Describe the bug

Kitty 0.32.2 successfully appends an animation frame sent in two direct-transfer
chunks, but emits no acknowledgement when the final chunk contains the documented
`a=f,m=0` controls without an image id. The client consequently times out waiting
for the graphics response.

The same pixels sent in a single chunk receive `OK`. Repeating `i=<image id>`
on the final chunk also receives `OK`. The latter response identifies frame 4,
which shows that the preceding silent upload created frame 3; it was not rejected.

## To reproduce

Run the following Bash script directly inside Kitty. It needs only Bash and
`stty`; no Icod libraries, .NET, image files, or graphics placements are involved.
It allocates a 2x1 RGB image, then tests single-chunk, documented two-chunk, and
final-chunk-identifier transfers. Each read has a two-second timeout. Received
escape sequences are printed safely using `%q`. The script removes only its
allocated image and restores terminal settings on exit.

```bash
#!/usr/bin/env bash
# Minimal Kitty animation-frame acknowledgement probe. No Icod dependencies.
# Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>

set -eu
exec 3<>/dev/tty
saved_tty=$(stty -g <&3)
image_id=''

send() { printf '\033_G%s\033\\' "$1" >&3; }
cleanup() {
    if [[ -n $image_id ]]; then
        send "a=d,d=I,i=$image_id,q=2"
    fi
    stty "$saved_tty" <&3
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
stty -echo -icanon min 1 time 0 <&3

receive() {
    reply=''
    if IFS= read -r -d '\' -t 2 reply <&3; then
        printf '  RX: %q\n' "$reply\\"
        return 0
    fi
    printf '  TIMEOUT (2 seconds); partial RX: %q\n' "$reply"
    return 1
}

# Ask Kitty to allocate an image id instead of overwriting a caller-chosen id.
# AAAAAAAA is six zero bytes: a 2x1 RGB image, deliberately never displayed.
printf 'Root image (2x1 RGB)\n'
send "a=t,I=$$,f=24,s=2,v=1,m=0;AAAAAAAA"
receive || exit 1
image_id=${reply#*$'Gi='}
image_id=${image_id%%,*}
if [[ ! $image_id =~ ^[1-9][0-9]*$ || $reply != *';OK'$'\e' ]]; then
    image_id=''
    printf 'Root creation did not return the expected allocated image id and OK.\n' >&2
    exit 1
fi

printf '1. Single chunk (control)\n'
send "a=f,i=$image_id,f=24,s=2,v=1,m=0;AAAAAAAA"
receive || :

printf '2. Two chunks: final a=f,m=0 (documented protocol)\n'
send "a=f,i=$image_id,f=24,s=2,v=1,m=1;AAAA"
send 'a=f,m=0;AAAA'
receive || :

printf '3. Two chunks: final a=f,i=<id>,m=0 (workaround probe)\n'
send "a=f,i=$image_id,f=24,s=2,v=1,m=1;AAAA"
send "a=f,i=$image_id,m=0;AAAA"
receive || :
```

The committed reproducer is also available at:
https://github.com/uniblab/Icod.DCurses/blob/ff20558e82780428dd5d6af9601f2a48ceffbe15/tools/kitty-frame-ack.sh

## Expected behavior

A completed, successful animation-frame upload should receive a correlated
`OK` response when responses have not been suppressed, regardless of whether
the direct-transfer payload needs one chunk or multiple chunks.

The [remote-client section of the graphics protocol](https://sw.kovidgoyal.net/kitty/graphics-protocol/#remote-client)
describes continuation chunks with `m`, optionally `q`, and `a=f` for animation
data. Therefore case 2 follows the documented continuation format; case 3
deliberately adds an identifier solely as a diagnostic comparison.

## Actual behavior / captured output

```text
Root image (2x1 RGB)
  RX: $'\E_Gi=1,I=6682;OK\E\\'
1. Single chunk (control)
  RX: $'\E_Gi=1,r=2;OK\E\\'
2. Two chunks: final a=f,m=0 (documented protocol)
  TIMEOUT (2 seconds); partial RX: ''
3. Two chunks: final a=f,i=<id>,m=0 (workaround probe)
  RX: $'\E_Gi=1,r=4;OK\E\\'
```

## Environment details

- `kitty --version`: `kitty 0.32.2 created by Kovid Goyal`.
- WSL2 / Ubuntu 24.04 on Windows 10.
- Live result captured on October 4, 2026.
- Full `kitty --debug-config` output was not collected.
- A clean `kitty --config NONE` run and a live retest on a newer Kitty version
  have not been performed; this report does not claim either.

## Source investigation and additional context

In [v0.32.2's graphics.c](https://github.com/kovidgoyal/kitty/blob/v0.32.2/kitty/graphics.c),
the `a=f` branch copies the incoming command to `ag`. The frame-loading handler
uses the saved start command for continuation data, but that does not replace
the caller's `ag`. Response construction then uses `ag`, whose image id/number
are zero for the documented final continuation. `finish_command_response`
returns no response when both identifiers are zero. This agrees with the
independent runtime result above.

Source inspection of
[commit 79f687dc5ca4f9583a064d59589337273cc57532](https://github.com/kovidgoyal/kitty/blob/79f687dc5ca4f9583a064d59589337273cc57532/kitty/graphics.c)
shows identifier recovery for continuations with no explicit action, while the
documented continuation with explicit `a=f` still follows the path described
above. This is source-based evidence, not a live reproduction on that revision.

This was first encountered while creating a persistent raster atlas: the root
image and placement succeeded, but waiting for the appended frame's ACK timed
out. The two-pixel standalone reproducer isolates the protocol behavior from
application rendering, payload size, transfer speed, and response matching.

This report is limited to the missing animation-upload acknowledgement. It does
not assert anything about animation-control success replies or rendering flicker.
