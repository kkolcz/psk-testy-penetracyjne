#!/bin/bash

echo "[*] Starting Apache..."
service apache2 start

echo "[*] Starting OpenSSH Server..."
ssh-keygen -A
/usr/sbin/sshd

tail -f /dev/null