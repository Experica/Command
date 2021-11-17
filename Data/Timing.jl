using FileIO,LinearAlgebra,Plots

## Get Test Data
cd(@__DIR__)
displayname = "ROGPG279Q"
datadir = "./$displayname"
figfmt = [".png",".svg"]

dataset = load(joinpath(datadir,"Test_CondTest_Duration.jld2"))

## Duration Plots
title = "From Photodiode Detected Lag-Corrected Signal\n(VSync=On, GSync=On, RefreshRate=144Hz)"
ylabel = "Condition Duration ($(round(Int,dataset["conddur1"]))ms)"
plot(dataset["ctdur1"];xlabel="Condition Tests",ylabel,title,leg=false)

foreach(ext->savefig(joinpath(datadir,"$ylabel$ext")),figfmt)
