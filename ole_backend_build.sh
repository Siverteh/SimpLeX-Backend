#!/bin/bash

docker build -t siverteh/simplex-backend:ole .
docker push siverteh/simplex-backend:ole
kubectl delete pods -l app=simplex-backend --wait=false
sleep 5
minikube service simplex-backend-service