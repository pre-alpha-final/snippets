#!/bin/bash

FILELIST=$(git rev-list --all --objects | awk '{print $1}' | git cat-file --batch-check | sort -k3nr)
echo "$FILELIST" | while read item
do
  HASH=$(echo $item | sed 's/\(^[^ ]*\).*/\1/')
  FILENAME=$(git rev-list --all --objects | grep "$HASH" | sed 's/^[^ ]* //')
  REST=$(echo $item | sed 's/^[^ ]*\(.*\)/\1/')
  echo "$FILENAME$REST"
done
