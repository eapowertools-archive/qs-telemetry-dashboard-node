@echo off
title Download Extensions Batch Script.

if not exist ./Resources/cl-container.zip curl -L https://github.com/ClimberAB/ClimberContainer/releases/download/v1.0.5/cl-container-v1.0.5.zip > ./Resources/cl-container.zip

if not exist ./Resources/cl-kpi.zip curl -L https://github.com/ClimberAB/ClimberKPI/releases/download/v1.4.5/cl-kpi_v1.4.5.zip > ./Resources/cl-kpi.zip

if not exist ./Resources/qsSimpleKPI.zip curl -L https://github.com/alner/qsSimpleKPI/raw/master/build/qsSimpleKPI.zip > ./Resources/qsSimpleKPI.zip

if not exist ./Resources/sense-navigation.zip curl -L https://github.com/stefanwalther/sense-navigation/raw/master/build/sense-navigation_latest.zip > ./Resources/sense-navigation.zip
