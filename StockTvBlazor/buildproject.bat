@echo off

docker build -t stocktv .
docker run -p 8080:8080 stocktv
