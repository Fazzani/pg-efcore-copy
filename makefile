pack:
	dotnet pack EF.Extensions.PgCopy/ -p:PackageVersion="1.0.0" -c Release -o out
push:
	dotnet nuget push out/EF.Extensions.PgCopy.1.0.0.nupkg -k $(NUGET_PAT2) -s https://api.nuget.org/v3/index.json
build_pg:
	docker build -f pg.Dockerfile -t pg-ef-copy:latest .
run_pg:
	docker run -itd --rm -e POSTGRES_PASSWORD=dt_pass -p 54322:5432 pg-ef-copy
ef_mig:
	chmod +x ./start.sh && ./start.sh -m
setup_db_for_test: build_pg run_pg
	dotnet ef database update -s TestConsoleApp
    