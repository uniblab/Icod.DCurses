# Minimal Kitty animation-frame acknowledgement reproducer

Run directly in Kitty on Linux/WSL or macOS, from the repository root:

```sh
bash tools/kitty-frame-ack.sh
```

Only Bash and `stty` are needed. There is no .NET, Icod.Terminal, or
Icod.DCurses dependency. Do not type while the probe is running. It normally
takes about two seconds on an affected Kitty, and at most eight seconds if all
four reads time out. `Ctrl+C` exits and restores terminal settings.

The script allocates an undisplayed 2x1 RGB image using `I=<image number>` and
reads Kitty's allocated `i=<image id>`. It then appends three identical frames:

| Case | First chunk | Final chunk | Hypothesized result on affected Kitty |
| --- | --- | --- | --- |
| 1: single chunk | Full controls and eight Base64 bytes, `m=0` | Not applicable | `OK` |
| 2: documented two-chunk transfer | Full controls and four Base64 bytes, `m=1` | `a=f,m=0;AAAA` | No response; timeout |
| 3: identifier workaround probe | Same as case 2 | `a=f,i=<id>,m=0;AAAA` | `OK` |

`AAAAAAAA` decodes to six zero bytes (two black RGB pixels). Each four-byte
Base64 chunk is a valid independently encoded three-byte group. The tiny
payload removes atlas size, rendering, throughput, input routing, and library
response matching from the experiment. No placement is created. On exit, only
the allocated image and its frames are deleted, and the saved terminal settings
are restored.

The [Kitty graphics protocol](https://sw.kovidgoyal.net/kitty/graphics-protocol/#remote-client)
requires animation continuations to carry `a=f`, with `m` and optionally `q`;
case 3 intentionally repeats an extra identifier to investigate the suspected
response-construction defect. It is an experiment, not a production workaround
or a claim that the workaround is portable to other terminals.

All received bytes are printed with Bash's `%q`, so escape sequences are
visible but never replayed as terminal commands. A timeout is an observation,
not proof that the frame was rejected or accepted. If case 1 succeeds, case 2
times out, and case 3 succeeds, that isolates the final-chunk identifier as the
acknowledgement difference. A negative reply or a different pattern warrants
further investigation before changing the libraries.

For a Kitty issue report, include `kitty --version`, the probe's complete
output, and `kitty-frame-ack.sh`. Running through tmux, screen, or another
terminal proxy introduces another variable; run directly in Kitty first.

## Confirmed result and unresolved status (2026-10-04)

The maintainer ran this probe in Kitty 0.32.2 on WSL2 / Ubuntu 24.04, Windows 10.
Root creation returned `Gi=1,I=6682;OK`; the single-chunk append returned
`Gi=1,r=2;OK`; the documented two-chunk append timed out with no input bytes;
and the final-chunk-identifier comparison returned `Gi=1,r=4;OK`.
The frame-4 response shows that the silent transfer created frame 3 successfully.
This independently reproduces the missing acknowledgement without either Icod
library. It does not qualify persistent ATLAS rendering or explain FRAME flicker.

The complete [upstream bug report](kitty-frame-ack-bug-report.md) includes the
standalone script, exact output, environment, protocol expectation, and source
analysis. Upstream filing was attempted, but GitHub returned HTTP 403,
`Resource not accessible by integration`; no upstream issue was created.
Status remains **unresolved**, as requested by the maintainer. No production
workaround or alpha.6 dependency change is authorized by this report. Further
probe expansion and library fixes are deferred.
