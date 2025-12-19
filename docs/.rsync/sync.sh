#! /usr/bin/env bash

rsync -av --include-from=.rsync/sync-list --exclude=.rsync . /mnt/c/Users/mathias.capdet/AppData/Local/_data/Obsidian\ vaults/New\ structure/
