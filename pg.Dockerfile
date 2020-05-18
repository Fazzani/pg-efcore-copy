FROM postgres

COPY init-user-db.sh /docker-entrypoint-initdb.d/init-user-db.sh