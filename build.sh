#!/usr/bin/env bash

rm -rf _out

# Generate a random ID for the build, to be used for finding the build in the docker host.
# This magic is taken from this stack overflow answer - https://stackoverflow.com/a/34329799/1189542.
BUILD_ID="$(od  -vN "8" -An -tx1  /dev/urandom | tr -d " \n")"

echo "Build ID: ${BUILD_ID}"

docker buildx build --tag "${BUILD_ID}" . && docker run --name "${BUILD_ID}" --privileged "${BUILD_ID}"

EXIT_CODE=$?
 	
BUILD_CONTAINER=${BUILD_ID}

docker container cp "${BUILD_CONTAINER}":/out ./_out
docker container rm "${BUILD_CONTAINER}" -v
docker image rm "${BUILD_ID}"

exit "$EXIT_CODE"