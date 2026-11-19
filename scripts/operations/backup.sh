#!/bin/bash

timestamp=$(date +%s)

rclone copy servervault/ $BACKUP_LOCATION/$timestamp/servervault/
rclone copy database/ $BACKUP_LOCATION/$timestamp/database/
rclone copy redis/ $BACKUP_LOCATION/$timestamp/redis/