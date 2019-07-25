using Plots,YAML,Makie,StatsMakie
include("algorithm.jl")

"Get RGB color spectral measured from a specific display"
function RGBSpectral(measurement)
    C = map(i->parse.(Float64,split(i)),measurement["Color"])
    λ = measurement["WL"]
    I = measurement["Spectral"]
    return C,λ,I
end

# plot cone fundamentals
Plots.plot(linss2_10e[:,1],linss2_10e[:,2:end],linewidth=2,color=["Red" "Green" "Blue"],xlabel="Wavelength (nm)",ylabel="Sensitivity",label=["L" "M" "S"],title="Cone Fundamentals(2deg)")
foreach(i->savefig("Cone Fundamentals(2deg)$i"),[".png",".svg"])

Plots.plot(linss10e[:,1],linss10e[:,2:end],linewidth=2,color=["Red" "Green" "Blue"],xlabel="Wavelength (nm)",ylabel="Sensitivity",label=["L" "M" "S"],title="Cone Fundamentals(10deg)")
foreach(i->savefig("Cone Fundamentals(10deg)$i"),[".png",".svg"])

# plot rgb color matching funcionts
Plots.plot(sbrgb2[:,1],sbrgb2[:,2:end],linewidth=2,color=["Red" "Green" "Blue"],xlabel="Wavelength (nm)",ylabel="Tristimulus Value",label=["r" "g" "b"],title="Color Matching Functions_rgb(2deg)")
Plots.scatter!(primary_rgb',zeros(3)',markersize=8,markerstrokewidth=0,markercolor=["Red" "Green" "Blue"],legend=false)
foreach(i->savefig("Color Matching Functions_rgb(2deg)$i"),[".png",".svg"])

Plots.plot(sbrgb10[:,1],sbrgb10[:,2:end],linewidth=2,color=["Red" "Green" "Blue"],xlabel="Wavelength (nm)",ylabel="Tristimulus Value",label=["r" "g" "b"],title="Color Matching Functions_rgb(10deg)")
Plots.scatter!(primary_rgb',zeros(3)',markersize=8,markerstrokewidth=0,markercolor=["Red" "Green" "Blue"],legend=false)
foreach(i->savefig("Color Matching Functions_rgb(10deg)$i"),[".png",".svg"])

# plot xyz color matching funcionts
xyz2 = getcmf(linss2_10e,LMSToXYZ2)
Plots.plot(xyz2[:,1],xyz2[:,2:end],linewidth=2,color=["Red" "Green" "Blue"],xlabel="Wavelength (nm)",ylabel="Tristimulus Value",label=["x" "y" "z"],title="Color Matching Functions_xyz(2deg)")
foreach(i->savefig("Color Matching Functions_xyz(2deg)$i"),[".png",".svg"])

xyz10 = getcmf(linss10e,LMSToXYZ10)
Plots.plot(xyz10[:,1],xyz10[:,2:end],linewidth=2,color=["Red" "Green" "Blue"],xlabel="Wavelength (nm)",ylabel="Tristimulus Value",label=["x" "y" "z"],title="Color Matching Functions_xyz(10deg)")
foreach(i->savefig("Color Matching Functions_xyz(10deg)$i"),[".png",".svg"])



# ViewSonic VX3276mhd IPS LCD
displayname = "VX3276mhd"
# Sony Trinitron CRT
displayname = "Trinitron"

resultdir = "./$displayname"
mkpath(resultdir)

config = YAML.load_file("CommandConfig_SNL-C.yaml")
RGBToLMS,LMSToRGB = RGBLMSMatrix(RGBSpectral(config["Display"][displayname]["SpectralMeasurement"])...)
RGBToXYZ,XYZToRGB = RGBXYZMatrix(RGBSpectral(config["Display"][displayname]["SpectralMeasurement"])...)


# unit cube colors
ur=0:0.01:1
uc=[[i,j,k] for i=ur,j=ur,k=ur][:]
ucm=hcat(uc...)
uqcm=quavectors(ucm)

# Transformation between specific display RGB and LMS Spaces
lms_rgb = RGBToLMS*uqcm
rgb_lms = LMSToRGB*uqcm

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGB(i...) for i in uc],markersize=0.01,transparency=true)
s.center=false
record(s,"Unit Color Space.mp4",1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("R / L","G / M","B / S")
record(s,joinpath(resultdir,"Unit RGB To LMS Space.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(rgb_lms[1,:],rgb_lms[2,:],rgb_lms[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("R / L","G / M","B / S")
record(s,joinpath(resultdir,"Unit LMS To RGB Space.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

# Transformation between specific display RGB and CIE XYZ Spaces
XYZ_rgb = RGBToXYZ*uqcm
rgb_XYZ = XYZToRGB*uqcm

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(XYZ_rgb[1,:],XYZ_rgb[2,:],XYZ_rgb[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("R / X","G / Y","B / Z")
record(s,joinpath(resultdir,"Unit RGB To XYZ Space.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(rgb_XYZ[1,:],rgb_XYZ[2,:],rgb_XYZ[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("R / X","G / Y","B / Z")
record(s,joinpath(resultdir,"Unit XYZ To RGB Space.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

xyY_rgb = XYZ2xyY(XYZ_rgb)

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(xyY_rgb[1,:],xyY_rgb[2,:],xyY_rgb[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)


# Transformation between LMS and Cone Contrast
bg = [0.5,0.5,0.5]
LMSToContrast,ContrastToLMS = LMSContrastMatrix(bg)
cc_lms = LMSToContrast*uqcm
lms_cc = ContrastToLMS*uqcm

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(cc_lms[1,:],cc_lms[2,:],cc_lms[3,:],color=[RGBA(i...,0.1) for i in uc],markersize=0.01,transparency=true)

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(lms_cc[1,:],lms_cc[2,:],lms_cc[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)

bg = RGBToLMS*[0.5,0.5,0.5,1]
LMSToContrast,ContrastToLMS = LMSContrastMatrix(bg)
cc_lms = LMSToContrast*lms_rgb

s=Makie.scatter(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(cc_lms[1,:],cc_lms[2,:],cc_lms[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)


# Transformation between LMS and DKL Spaces
bg = [0.5,0.5,0.5]
LMSToDKL,DKLToLMS = LMSDKLMatrix(bg,isnorm=true)
dkl_lms = LMSToDKL*uqcm
lms_dkl = DKLToLMS*uqcm

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(dkl_lms[1,:],dkl_lms[2,:],dkl_lms[3,:],color=[RGBA(i...,0.1) for i in uc],markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("L / L+M","M / L-M","S / S-(L+M)")
record(s,"Unit LMS To DKL$bg Space.mp4",1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(lms_dkl[1,:],lms_dkl[2,:],lms_dkl[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("L / L+M","M / L-M","S / S-(L+M)")

bg = RGBToLMS*[0.5,0.5,0.5,1]
LMSToDKL,DKLToLMS = LMSDKLMatrix(bg,isnorm=true)
dkl_lms = LMSToDKL*lms_rgb

s=Makie.scatter(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(dkl_lms[1,:],dkl_lms[2,:],dkl_lms[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("L / L+M","M / L-M","S / S-(L+M)")
record(s,joinpath(resultdir,"LMS To DKL$bg Space.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end



# Cone Isolating Modulation Range
lms_gray= RGBToLMS*[0.5,0.5,0.5,1]

s=Makie.surface(StatsMakie.density,lms_rgb[2,:],lms_rgb[3,:],colormap=:reds)
lines!([0,0].+lms_gray[2],[0,0].+lms_gray[3],[0,1],linewidth=3,color=:gray)
s.center=false
s[Axis][:names,:axisnames]=("M","S","Liso Strength")

s=Makie.surface(StatsMakie.density,lms_rgb[1,:],lms_rgb[3,:],colormap=:greens)
lines!([0,0].+lms_gray[1],[0,0].+lms_gray[3],[0,1],linewidth=3,color=:gray)
s.center=false
s[Axis][:names,:axisnames]=("L","S","Miso Strength")

s=Makie.surface(StatsMakie.density,lms_rgb[1,:],lms_rgb[2,:],colormap=:blues)
lines!([0,0].+lms_gray[1],[0,0].+lms_gray[2],[0,1],linewidth=3,color=:gray)
s.center=false
s[Axis][:names,:axisnames]=("L","M","Siso Strength")

# Cone Isolating RGB through a color
th = [0.5,0.5,0.5]
# each column of LMSToRGB is the cone isolating RGB direction
ConeIsoRGBVec = trimatrix(LMSToRGB)
# Scale RGB Vector into Unit Cube
ConeIsoRGBVec./=maximum(abs.(ConeIsoRGBVec),dims=1)
# since through color is the center of RGB cube, the line intersects at two symmatric points on the faces of unit cube
minc = th.-0.5*ConeIsoRGBVec;maxc = th.+0.5*ConeIsoRGBVec
ConeIsoRGB =map((i,j)->collect(i.+j), minc,[0:0.0001:1].*(maxc.-minc))

Liso = hcat(ConeIsoRGB[:,1]...)'
Lisoc = [RGB(Liso[:,i]...) for i in 1:size(Liso,2)]
Lisolms=RGBToLMS*quavectors(Liso)

Miso = hcat(ConeIsoRGB[:,2]...)'
Misoc = [RGB(Miso[:,i]...) for i in 1:size(Miso,2)]
Misolms=RGBToLMS*quavectors(Miso)

Siso = hcat(ConeIsoRGB[:,3]...)'
Sisoc = [RGB(Siso[:,i]...) for i in 1:size(Siso,2)]
Sisolms=RGBToLMS*quavectors(Siso)


s=Makie.scatter(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(Lisolms[1,:],Lisolms[2,:],Lisolms[3,:],color=Lisoc,markersize=0.01,transparency=true)
Makie.scatter!(Misolms[1,:],Misolms[2,:],Misolms[3,:],color=Misoc,markersize=0.01,transparency=true)
Makie.scatter!(Sisolms[1,:],Sisolms[2,:],Sisolms[3,:],color=Sisoc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("L","M","S")
record(s,joinpath(resultdir,"ConeIsolating_ThroughGray_LMSSpace.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(Liso[1,:],Liso[2,:],Liso[3,:],color=Lisoc,markersize=0.01,transparency=true)
Makie.scatter!(Miso[1,:],Miso[2,:],Miso[3,:],color=Misoc,markersize=0.01,transparency=true)
Makie.scatter!(Siso[1,:],Siso[2,:],Siso[3,:],color=Sisoc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("R","G","B")
record(s,joinpath(resultdir,"ConeIsolating_ThroughGray_RGBSpace.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end


# DKL Isolating RGB through a background color
bg = RGBToLMS*[th;1]
LMSToDKL,DKLToLMS = LMSDKLMatrix(bg,isnorm=true)
# convert each column of DKLToLMS which are the DKL isolating LMS to RGB direction
DKLIsoRGBVec = trimatrix(LMSToRGB*DKLToLMS)
# Scale RGB Vector into Unit Cube
DKLIsoRGBVec./=maximum(abs.(DKLIsoRGBVec),dims=1)
# since through color is the center of RGB cube, the line intersects at two symmatric points on the faces of unit cube
minc = th.-0.5*DKLIsoRGBVec;maxc = th.+0.5*DKLIsoRGBVec
DKLIsoRGB =map((i,j)->collect(i.+j), minc,[0:0.0001:1].*(maxc.-minc))

Lumiso = hcat(DKLIsoRGB[:,1]...)'
Lumisoc = [RGB(Lumiso[:,i]...) for i in 1:size(Lumiso,2)]
Lumisolms=RGBToLMS*quavectors(Lumiso)
Lumisodkl = LMSToDKL*Lumisolms

LMiso = hcat(DKLIsoRGB[:,2]...)'
LMisoc = [RGB(LMiso[:,i]...) for i in 1:size(LMiso,2)]
LMisolms=RGBToLMS*quavectors(LMiso)
LMisodkl = LMSToDKL*LMisolms

SLMiso = hcat(DKLIsoRGB[:,3]...)'
SLMisoc = [RGB(SLMiso[:,i]...) for i in 1:size(SLMiso,2)]
SLMisolms=RGBToLMS*quavectors(SLMiso)
SLMisodkl = LMSToDKL*SLMisolms

s=Makie.scatter(dkl_lms[1,:],dkl_lms[2,:],dkl_lms[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(Lumisodkl[1,:],Lumisodkl[2,:],Lumisodkl[3,:],color=Lumisoc,markersize=0.01,transparency=true)
Makie.scatter!(LMisodkl[1,:],LMisodkl[2,:],LMisodkl[3,:],color=LMisoc,markersize=0.01,transparency=true)
Makie.scatter!(SLMisodkl[1,:],SLMisodkl[2,:],SLMisodkl[3,:],color=SLMisoc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("L+M","L-M","S-(L+M)")
record(s,joinpath(resultdir,"DKLIsolating_ThroughGray_DKLSpace.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(Lumisolms[1,:],Lumisolms[2,:],Lumisolms[3,:],color=Lumisoc,markersize=0.01,transparency=true)
Makie.scatter!(LMisolms[1,:],LMisolms[2,:],LMisolms[3,:],color=LMisoc,markersize=0.01,transparency=true)
Makie.scatter!(SLMisolms[1,:],SLMisolms[2,:],SLMisolms[3,:],color=SLMisoc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("L","M","S")
record(s,joinpath(resultdir,"DKLIsolating_ThroughGray_LMSSpace.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(Lumiso[1,:],Lumiso[2,:],Lumiso[3,:],color=Lumisoc,markersize=0.01,transparency=true)
Makie.scatter!(LMiso[1,:],LMiso[2,:],LMiso[3,:],color=LMisoc,markersize=0.01,transparency=true)
Makie.scatter!(SLMiso[1,:],SLMiso[2,:],SLMiso[3,:],color=SLMisoc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("R","G","B")
record(s,joinpath(resultdir,"DKLIsolating_ThroughGray_RGBSpace.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end


# DKL Isoluminance plane
DKLToRGB = LMSToRGB*DKLToLMS
RGBToDKL = LMSToDKL*RGBToLMS
lum = 0
# rotate l-m direction around l+m axis within the Isoluminance plane
DKLIsoLumRGBVec = trivectors(hcat(map(i->DKLToRGB*RotateXYZMatrix(deg2rad(i),dims=1)*[0,1,0,0], 0:0.05:179.95)...))
# find Intersections of Isoluminance directions with faces of unit RGB cube
minmaxc = hcat(map(i->intersectlineunitorigincube((DKLToRGB*[lum,0,0,1])[1:3],DKLIsoLumRGBVec[:,i]),1:size(DKLIsoLumRGBVec,2))...)
minc=minmaxc[:,1:2:end];maxc=minmaxc[:,2:2:end]
DKLIsoLumRGB =map((i,j)->collect(i.+j), minc,[0:0.001:1].*(maxc.-minc))

IsoLum=hcat(map(i->hcat(DKLIsoLumRGB[:,i]...)',1:size(DKLIsoLumRGB,2))...)
IsoLumc = [RGB(IsoLum[:,i]...) for i in 1:size(IsoLum,2)]
IsoLumlms=RGBToLMS*quavectors(IsoLum)
IsoLumdkl = LMSToDKL*IsoLumlms

s=Makie.scatter(IsoLumdkl[2,:],IsoLumdkl[3,:],color=IsoLumc,markersize=0.01,scale_plot=false)
s[Axis][:names,:axisnames]=("L-M","S-(L+M)")
s=title(s,"DKL Isoluminance Plane through $lum")
s.center=false
Makie.save(joinpath(resultdir,"DKL Isoluminance Plane through $lum.png"),s)

# combine several Isoluminance planes
IsoLum = hcat(IsoLum,hcat(map(i->hcat(DKLIsoLumRGB[:,i]...)',1:size(DKLIsoLumRGB,2))...))

IsoLumc = [RGB(IsoLum[:,i]...) for i in 1:size(IsoLum,2)]
IsoLumlms=RGBToLMS*quavectors(IsoLum)
IsoLumdkl = LMSToDKL*IsoLumlms

s=Makie.scatter(dkl_lms[1,:],dkl_lms[2,:],dkl_lms[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(IsoLumdkl[1,:],IsoLumdkl[2,:],IsoLumdkl[3,:],color=IsoLumc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("L+M","L-M","S-(L+M)")
record(s,joinpath(resultdir,"DKLIsoluminancePlane_DKLSpace.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(IsoLumlms[1,:],IsoLumlms[2,:],IsoLumlms[3,:],color=IsoLumc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("L","M","S")
record(s,joinpath(resultdir,"DKLIsoluminancePlane_LMSSpace.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(IsoLum[1,:],IsoLum[2,:],IsoLum[3,:],color=IsoLumc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("R","G","B")
record(s,joinpath(resultdir,"DKLIsoluminancePlane_RGBSpace.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end



# rgb color matching chromaticity
ploc = divsum(hcat([lambdamatch(w,sbrgb10) for w in primary_rgb]...))
wloc = divsum(hcat([lambdamatch(w,sbrgb10) for w in 390:830]...))

s=Makie.lines(wloc[1,:],wloc[2,:],wloc[3,:],color=:black,linewidth=3,transparency=true)
Makie.scatter!(ploc[1,:],ploc[2,:],ploc[3,:],color=[:red,:green,:blue],markersize=0.05,transparency=true)
Makie.lines!(wloc[1,:],wloc[2,:],color=:gray40,linewidth=3,transparency=true)
Makie.scatter!(ploc[1,:],ploc[2,:],color=[:red,:green,:blue],markersize=0.05,transparency=true)

aw =  [450,500,550,600]
awloc = divsum(hcat([lambdamatch(w,sbrgb10) for w in aw]...))
Makie.scatter!(awloc[1,:],awloc[2,:],color=:gray20,markersize=0.03,transparency=true)
annotations!(string.(" ",aw),Point2.(awloc[1,:],awloc[2,:]),textsize=0.08)
s[Axis][:names,:axisnames]=("r","g","b")
record(s,"Color Matching Chromaticity_rgb.mp4",1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end


# xyz color matching chromaticity
wloc = divsum(hcat([lambdamatch(w,xyz10) for w in 390:830]...))
aw =  [450,500,550,600,650]
awloc = divsum(hcat([lambdamatch(w,xyz10) for w in aw]...))
xyz_rgb =divsum(trivectors(XYZ_rgb))

s=Makie.lines(wloc[1,:],wloc[2,:],color=:black,linewidth=1.5,limits = FRect(0,0,0.8,0.8))
Makie.scatter!(awloc[1,:],awloc[2,:],color=:black,markersize=0.01)
annotations!(string.(aw),[Point2(awloc[1,i]>0.3 ? awloc[1,i]+0.01 : awloc[1,i]-0.05,awloc[2,i]) for i in 1:length(aw)],textsize=0.02)
Makie.scatter!(xyz_rgb[1,:],xyz_rgb[2,:],color=[RGB(i...) for i in uc],markersize=0.01,transparency=true,scale_plot=false)
s=title(s,"RGB gamut in CIE xy chromaticity")
s.center=false
Makie.save(joinpath(resultdir,"RGB gamut in CIE xy chromaticity.png"),s)




function linepoints(p1,p2,d)
    dir = p2-p1
    ll =norm(dir)
    hcat([p1.+λ*dir for λ in 0:ll/d:1]...)
end

wloc = divsum(hcat([lambdamatch(w,xyz10) for w in 390:0.2:830]...))
t=hcat([linepoints(wloc[1:2,1],wloc[1:2,i],100) for i in 2:size(wloc,2)]...)
txyz = vcat(t,1 .- sum(t,dims=1))
trgb_xyz = desaturate(trimatrix(XYZToRGB)*txyz)


Makie.scatter!(t[1,:],t[2,:],color=[RGBA(trgb_xyz[:,i]...,1) for i in 1:size(trgb_xyz,2)],markersize=0.005,transparency=true)



vc=[[i,j,1-i-j] for i=0.2:0.01:0.4,j=0.2:0.01:0.5][:]
vcm=hcat(vc...)
vqcm = quavectors(vcm)

rgb_vc = trivectors(XYZToRGB*vqcm)

drgb_vc = desaturate(rgb_vc)

function desaturate(m)
    for i in 1:size(m,2)
        minc = minimum(m[:,i])
        if minc<0
            m[:,i]=m[:,i].-minc
        end
        maxc = maximum(m[:,i])
        if maxc>0
            m[:,i]=m[:,i]./maxc
        end
    end
    return m
end


Makie.scatter(rgb_vc[1,:],rgb_vc[2,:],rgb_vc[3,:],color=[RGBA(rgb_vc[:,i]...,1) for i in 1:size(rgb_vc,2)],markersize=0.01,transparency=true)

Makie.scatter(drgb_vc[1,:],drgb_vc[2,:],drgb_vc[3,:],color=[RGBA(drgb_vc[:,i]...,1) for i in 1:size(drgb_vc,2)],markersize=0.01,transparency=true)

Makie.scatter(vcm[1,:],vcm[2,:],color=[RGBA(rgb_vc[:,i]...,1) for i in 1:size(rgb_vc,2)],markersize=0.01,transparency=true)


Makie.scatter(vcm[1,:],vcm[2,:],color=[RGBA(drgb_vc[:,i]...,1) for i in 1:size(drgb_vc,2)],markersize=0.01,transparency=true)

RGBToxyz=divsum(trimatrix(RGBToXYZ))

xyz_rgb = divsum(tt*ucm)

xyz_white = divsum(trimatrix(RGBToXYZ)*ones(3))

xyzToRGB = inv(RGBToxyz)


xyzToRGB*xyz_white

t = xyzToRGB./((xyzToRGB*xyz_white))
t= xyzToRGB
tt = inv(t)


tt,t = RGBXYZMatrix(RGBSpectral(config["Display"][displayname]["SpectralMeasurement"])...)

xyz_rgb = divsum(trimatrix(tt)*ucm)



s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(XYZ_rgb[1,:],XYZ_rgb[2,:],XYZ_rgb[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)


s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(xyz_rgb[1,:],xyz_rgb[2,:],xyz_rgb[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)

s.center=false
s[Axis][:names,:axisnames]=("R / L","G / M","B / S")
record(s,joinpath(resultdir,"Unit RGB To LMS Space.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(rgb_lms[1,:],rgb_lms[2,:],rgb_lms[3,:],color=[RGBA(i...,1) for i in uc],markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("R / L","G / M","B / S")
record(s,joinpath(resultdir,"Unit LMS To RGB Space.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end






























tIsoLumdkl = TranslateXYZMatrix(x=-0.5)*IsoLumdkl
Makie.scatter!(tIsoLumdkl[1,:],tIsoLumdkl[2,:],tIsoLumdkl[3,:],color=IsoLumc,markersize=0.01,transparency=true)
tIsoLumrgb = trivectors(DKLToRGB*tIsoLumdkl)
tIsoLumrgb = clamp.(tIsoLumrgb,0,1)
tIsoLumrgb./=maximum(abs.(tIsoLumrgb),dims=1)
Makie.scatter!(tIsoLumrgb[1,:],tIsoLumrgb[2,:],tIsoLumrgb[3,:],color=IsoLumc,markersize=0.01,transparency=true)


DKLIsoLumRGBVec = trivectors(hcat([DKLToRGB*RotateXYZMatrix(deg2rad(i>90 ? -j : j),dims=3)*RotateXYZMatrix(deg2rad(i),dims=1)*[0,1,0,0] for i=0:1:180,j=[75]][:]...))

DKLIsoLumRGBVec./=maximum(abs.(DKLIsoLumRGBVec),dims=1)
minc = th.-0.5*DKLIsoLumRGBVec;maxc = th.+0.5*DKLIsoLumRGBVec
DKLIsoLumRGB =map((i,j)->collect(i.+j), minc,[0:0.01:1].*(maxc.-minc))

IsoLum=hcat(map(i->hcat(DKLIsoLumRGB[:,i]...)',1:size(DKLIsoLumRGB,2))...)
IsoLumc = [RGB(IsoLum[:,i]...) for i in 1:size(IsoLum,2)]
IsoLumlms=RGBToLMS*quavectors(IsoLum)
IsoLumdkl = LMSToDKL*IsoLumlms

s=Makie.scatter(dkl_lms[1,:],dkl_lms[2,:],dkl_lms[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(IsoLumdkl[1,:],IsoLumdkl[2,:],IsoLumdkl[3,:],color=IsoLumc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("L+M","L-M","S-(L+M)")

s=Makie.scatter(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(IsoLumlms[1,:],IsoLumlms[2,:],IsoLumlms[3,:],color=IsoLumc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("L","M","S")

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(IsoLum[1,:],IsoLum[2,:],IsoLum[3,:],color=IsoLumc,markersize=0.01,transparency=true)
s.center=false
s[Axis][:names,:axisnames]=("R","G","B")




t=Makie.scatter(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,0.01) for i in uc],markersize=0.01,transparency=true)
Makie.scatter!(IsoLumlms[1,:],IsoLumlms[2,:],IsoLumlms[3,:],color=IsoLumc,markersize=0.01,transparency=true)



s=Makie.scatter(minc[1,:],minc[2,:],minc[3,:],color=[RGBA(minc[:,i]...,1) for i in size(minc,2)],markersize=0.01,transparency=true)
Makie.scatter!(maxc[1,:],maxc[2,:],maxc[3,:],color=[RGBA(maxc[:,i]...,1) for i in size(maxc,2)],markersize=0.01,transparency=false)




s=Makie.scatter(minc[1,:],minc[2,:],minc[3,:],color=:black,markersize=0.01,transparency=true)

Makie.scatter!(s,maxc[1,:],maxc[2,:],maxc[3,:],color=:red,markersize=0.01,transparency=true)

t=mapreduce(i->hcat(minc[:,i],maxc[:,i]),hcat,1:size(minc,2))

linesegments!(s,t[1,:],t[2,:],t[3,:])

s=Makie.scatter(maxc[1,:],maxc[2,:],maxc[3,:],color=[RGB(maxc[:,i]...) for i in size(maxc,2)],markersize=10,transparency=false)

[RGB(minc[:,i]...) for i in 1:size(minc,2)]

[RGB(maxc[:,i]...) for i in 1:size(maxc,2)]


















# Transformation between differential LMS and DKL Spaces
bg = [0.5,0.5,0.5]
dLMSToDKL,DKLTodLMS = dLMSDKLMatrix(bg,isnorm=true)
dkl_lms = dLMSToDKL*(ucm.-bg)
lms_dkl = DKLTodLMS*(ucm.-bg)

s=Makie.scatter(ucm[1,:].-bg[1],ucm[2,:].-bg[2],ucm[3,:].-bg[3],color=[RGBA(i...,1) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(dkl_lms[1,:],dkl_lms[2,:],dkl_lms[3,:],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
s[Axis][:names,:axisnames]=("dL / L+M","dM / L-M","dS / S-(L+M)")
s.center=false
Makie.save("dLMS To DKL$bg Space.png",s)

record(s,"dLMS To DKL$bg Space.mp4",1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:].-bg[1],ucm[2,:].-bg[2],ucm[3,:].-bg[3],color=[RGBA(i...,1) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(lms_dkl[1,:],lms_dkl[2,:],lms_dkl[3,:],color=[RGBA(i...,1) for i in ucs],markersize=0.01,transparency=true)
s[Axis][:names,:axisnames]=("dL / L+M","dM / L-M","dS / S-(L+M)")
s.center=false
Makie.save("DKL$bg To dLMS Space.png",s)

record(s,"DKL$bg To dLMS Space.mp4",1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

bg = RGBToLMS*[0.5,0.5,0.5]
dLMSToDKL,DKLTodLMS = dLMSDKLMatrix(bg,isnorm=true)
dkl_lms = dLMSToDKL*(lms_rgb.-bg)

s=Makie.scatter(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,1) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(dkl_lms[1,:],dkl_lms[2,:],dkl_lms[3,:],color=[RGBA(i...,1) for i in ucs],markersize=0.01,transparency=true)
s[Axis][:names,:axisnames]=("L / L+M","M / L-M","S / S-(L+M)")
s.center=false
Makie.save(joinpath(resultdir,"LMS To DKL$bg Space.png"),s)

record(s,joinpath(resultdir,"LMS To DKL$bg Space.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

# DKL Isolating RGB Color through Gray [0.5, 0.5, 0.5]
bg = [0.5,0.5,0.5]
dLMSToDKL,DKLTodLMS = dLMSDKLMatrix(bg,isnorm=true)
DKLIsoLMS = DKLTodLMS*[1 0 0;
                       0 1 0;
                       0 0 1]

#DKLIsoLMS .+= bg
DKLIsoLMS./=maximum(abs.(DKLIsoLMS),dims=1)

mr = -0.5:0.001:0.5
Disos = hcat([collect(mr*DKLIsoLMS[i,1].+0.5) for i=1:3]...)'
Disosc=[RGB(Disos[:,i]...) for i in 1:size(Disos,2)]
Disosdkl=dLMSToDKL*(Disos.-bg)

Kisos = hcat([collect(mr*DKLIsoLMS[i,2].+0.5) for i=1:3]...)'
Kisosc=[RGB(Kisos[:,i]...) for i in 1:size(Kisos,2)]
Kisosdkl=dLMSToDKL*(Kisos.-bg)

Lisos = hcat([collect(mr*DKLIsoLMS[i,3].+0.5) for i=1:3]...)'
Lisosc = [RGB(Lisos[:,i]...) for i in 1:size(Lisos,2)]
Lisosdkl=dLMSToDKL*(Lisos.-bg)

s=Makie.scatter(dkl_lms[1,:],dkl_lms[2,:],dkl_lms[3,:],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
#Makie.scatter!(DKLTodLMS[1,:],DKLTodLMS[2,:],DKLTodLMS[3,:],color=:black,markersize=0.1,transparency=true)
#Makie.arrows!([0],[0],[0],DKLTodLMS[1:1,1],DKLTodLMS[2:2,1],DKLTodLMS[3:3,1])

Makie.scatter!(Disosdkl[1,:],Disosdkl[2,:],Disosdkl[3,:],color=Disosc,markersize=0.01,transparency=true)
Makie.scatter!(Kisosdkl[1,:],Kisosdkl[2,:],Kisosdkl[3,:],color=Kisosc,markersize=0.01,transparency=true)
Makie.scatter!(Lisosdkl[1,:],Lisosdkl[2,:],Lisosdkl[3,:],color=Lisosc,markersize=0.01,transparency=true)
s[Axis][:names,:axisnames]=("L","M","S")
s.center=false
Makie.save(joinpath(resultdir,"ConeIsolating_ThroughGray_LMSSpace.png"),s)
