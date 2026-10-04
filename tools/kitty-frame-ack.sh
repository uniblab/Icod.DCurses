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
