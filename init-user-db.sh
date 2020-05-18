#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE USER db_user;
    CREATE DATABASE dbtest;
    GRANT ALL PRIVILEGES ON DATABASE dbtest TO db_user;
    ALTER USER db_user PASSWORD 'dtpass';
EOSQL