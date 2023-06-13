#!/bin/sh

set -e

# Change .erlang.cookie permission
chmod 400 /var/lib/rabbitmq/.erlang.cookie
rabbitmq-plugins enable rabbitmq_mqtt
/usr/local/bin/docker-entrypoint.sh rabbitmq-server


