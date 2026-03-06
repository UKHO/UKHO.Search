# Pushing the local image

```bash
az login
az acr login --name searchacr

docker tag fss-data-vnext-e2e:latest searchacr.azurecr.io/fss-data-vnext-e2e:latest
docker push searchacr.azurecr.io/fss-data-vnext-e2e:latest
```

# Getting an image

```bash
az login
az acr login --name searchacr

docker pull searchacr.azurecr.io/fss-data-vnext-e2e:latest
docker tag searchacr.azurecr.io/fss-data-vnext-e2e:latest fss-data-vnext-e2e:latest
docker rmi searchacr.azurecr.io/fss-data-vnext-e2e:latest
```

