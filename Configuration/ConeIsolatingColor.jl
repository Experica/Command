using CSV,DataFrames,StatsPlots,YAML,Dierckx,LinearAlgebra,Makie

"Get RGB color spectral"
function RGBSpectral(measurement)
    C = map(i->parse.(Float64,split(i)[1:3]),measurement["Color"])
    λ = measurement["WL"]
    I = measurement["Spectral"]
    return C,λ,I
end
"Cone activations of RGB colors, based on color and cone spectral"
function RGB2LMS(C,λ,I,cones)
    cλ = cones[:r];CC=Vector{Vector{Float64}}(undef,length(C))
    for i in 1:length(C)
        s = Spline1D(λ[i],I[i],k=3,bc="zero")(cλ)'
        CC[i] = [s*cones[:L],s*cones[:M],s*cones[:S]]
    end
    return C,CC
end

# Cone Fundamentals from http://www.cvrl.org/cones.htm
# 2-deg fundamentals based on the Stiles & Burch 10-deg CMFs (adjusted to 2-deg), Stockman & Sharpe (2000), Linear Energy, 0.1nm
conef = "linss2_10e"
# 10-deg fundamentals based on the Stiles & Burch 10-deg CMFs, Stockman & Sharpe (2000), Linear Energy, 0.1nm
conef = "linss10e"

# Cone Fundamentals Prepared with Missing replaced to 0
linss10e = mapcols(x->coalesce.(x,0.0), CSV.read("$(conef)_fine.csv",header=[:r,:L,:M,:S]))
CSV.write("$(conef).csv",linss10e)

@df linss10e Plots.plot(:r,[:L,:M,:S],color=["Red" "Green" "Blue"],xlabel="Wavelength (nm)",ylabel="Sensitivity",label=["L" "M" "S"],title="Cone Fundamentals($conef)")
foreach(i->savefig("Cone Fundamentals($conef)$i"),[".png",".svg"])


config = YAML.load_file("CommandConfig_SNL-C.yaml")
# Cone responses of RGB colors
C,CC = RGB2LMS(RGBSpectral(config["Display"]["Trinitron"]["SpectralMeasurement"])...,linss10e)
# Solve System of Linear Equations in form of Space Converting Matrix
mc=hcat(C...);mcc=hcat(CC...)
ToLMS = mcc/mc
ToRGB = mc/mcc

# Transformation between RGB Color Space and LMS Color Space
ucrange=0:0.01:1
ucspace=[[i,j,k] for i=ucrange,j=ucrange,k=ucrange][:]
ucm=hcat(ucspace...)
mlms = ToLMS*ucm
mrgb = ToRGB*ucm

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,0.1) for i in ucspace],markersize=0.01,transparency=true)
Makie.save("RGB_LMS Space.png",s)
Makie.scatter!(mlms[1,:],mlms[2,:],mlms[3,:],color=[RGBA(i...,0.1) for i in ucspace],markersize=0.01,transparency=true)
Makie.save("RGB To LMS Space.png",s)

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,0.1) for i in ucspace],markersize=0.01,transparency=true)
Makie.scatter!(mrgb[1,:],mrgb[2,:],mrgb[3,:],color=[RGBA(i...,0.1) for i in ucspace],markersize=0.01,transparency=true)
Makie.save("LMS To RGB Space.png",s)

# Cone Isolating Modulation Range
midlms= ToLMS*[0.5,0.5,0.5]
histogram2d(mlms[2,:],mlms[3,:],nbins=60,color=:reds,title="Liso Strength",xlabel="M",ylabel="S")
vline!(midlms[2:2],color=:gray);hline!(midlms[3:3],color=:gray,legend=false)
foreach(i->savefig("Liso Strength$i"),[".png",".svg"])
histogram2d(mlms[1,:],mlms[3,:],nbins=60,color=:greens,title="Miso Strength",xlabel="L",ylabel="S")
vline!(midlms[1:1],color=:gray);hline!(midlms[3:3],color=:gray,legend=false)
foreach(i->savefig("Miso Strength$i"),[".png",".svg"])
histogram2d(mlms[1,:],mlms[2,:],nbins=60,color=:blues,title="Siso Strength",xlabel="L",ylabel="M")
vline!(midlms[1:1],color=:gray);hline!(midlms[2:2],color=:gray,legend=false)
foreach(i->savefig("Siso Strength$i"),[".png",".svg"])


# Cone Isolating RGB Color through Mid-Gray [0.5, 0.5, 0.5] from Cone Isolating Unit Vector in RGB Space
LMSiso = ToRGB*[1 0 0;
                0 1 0;
                0 0 1]
# Scale Each Cone Isolating Vector into Unit Cube
LMSiso./=maximum(abs.(LMSiso),dims=1)

cmrange = -0.5:0.001:0.5
Lisos = hcat([collect(cmrange*LMSiso[i,1].+0.5) for i=1:3]...)'
Lisosc=[RGB(Lisos[:,i]...) for i in 1:size(Lisos,2)]
Lisoslms=ToLMS*Lisos

Misos = hcat([collect(cmrange*LMSiso[i,2].+0.5) for i=1:3]...)'
Misosc=[RGB(Misos[:,i]...) for i in 1:size(Misos,2)]
Misoslms=ToLMS*Misos

Sisos = hcat([collect(cmrange*LMSiso[i,3].+0.5) for i=1:3]...)'
Sisosc = [RGB(Sisos[:,i]...) for i in 1:size(Sisos,2)]
Sisoslms=ToLMS*Sisos

s=Makie.scatter(mlms[1,:],mlms[2,:],mlms[3,:],color=[RGBA(i...,0.1) for i in ucspace],markersize=0.01,transparency=true)
Makie.scatter!(Lisoslms[1,:],Lisoslms[2,:],Lisoslms[3,:],color=Lisosc,markersize=0.01,transparency=true)
Makie.scatter!(Misoslms[1,:],Misoslms[2,:],Misoslms[3,:],color=Misosc,markersize=0.01,transparency=true)
Makie.scatter!(Sisoslms[1,:],Sisoslms[2,:],Sisoslms[3,:],color=Sisosc,markersize=0.01,transparency=true)
Makie.save("ConeIsolating_ThroughGray_LMSSpace.png",s)

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,0.1) for i in ucspace],markersize=0.01,transparency=true)
Makie.scatter!(Lisos[1,:],Lisos[2,:],Lisos[3,:],color=Lisosc,markersize=0.01,transparency=true)
Makie.scatter!(Misos[1,:],Misos[2,:],Misos[3,:],color=Misosc,markersize=0.01,transparency=true)
Makie.scatter!(Sisos[1,:],Sisos[2,:],Sisos[3,:],color=Sisosc,markersize=0.01,transparency=true)
Makie.save("ConeIsolating_ThroughGray_RGBSpace.png",s)
