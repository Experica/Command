using Colors,CSV,DataFrames,StatsPlots,YAML,Dierckx,LinearAlgebra,Makie

"Get RGB color spectral measured from a specific display"
function RGBSpectral(measurement)
    C = map(i->parse.(Float64,split(i)[1:3]),measurement["Color"])
    λ = measurement["WL"]
    I = measurement["Spectral"]
    return C,λ,I
end
"Cone activations of RGB colors, based on colors and cones spectral"
function RGB2LMS(C,λ,I,cones)
    r = cones[:r];CC=Vector{Vector{Float64}}(undef,length(C))
    for i in 1:length(C)
        s = Spline1D(λ[i],I[i],k=3,bc="zero")(r)'
        CC[i] = [s*cones[:L],s*cones[:M],s*cones[:S]]
    end
    return CC,C
end
"Converting Matrix between RGB and LMS Space, based on colors and cones spectral"
function RGBLMSMatrix(C,λ,I,cones)
    CC,C = RGB2LMS(C,λ,I,cones)
    # Solve System of Linear Equations in form of Space Converting Matrix
    mc=hcat(C...);mcc=hcat(CC...)
    RGBToLMS = mcc/mc
    LMSToRGB = mc/mcc
    return RGBToLMS,LMSToRGB
end
"""
Converting Affine Matrix between LMS and Cone Contrast Space
(DH Brainard, Cone contrast and opponent modulation color spaces, human color vision, 1996)
"""
function LMSContrastMatrix(bg)
    # translation to bg origin to get differential cone excitation
    t = [1 0 0 -bg[1];
         0 1 0 -bg[2];
         0 0 1 -bg[3];
         0 0 0 1]
    # scale relative to bg to get cone contrast
    s = [inv(bg[1]) 0 0 0;
         0 inv(bg[2]) 0 0;
         0 0 inv(bg[3]) 0;
         0 0 0 1]
    LMSToContrast = s*t
    ContrastToLMS = inv(LMSToContrast)
    return LMSToContrast,ContrastToLMS
end
"""
Converting Matrix between differential LMS and DKL[L+M, L-M, S-(L+M)] Space
(DH Brainard, Cone contrast and opponent modulation color spaces, human color vision, 1996)
"""
function dLMSDKLMatrix(bg;isnorm=true)
    dLMSToDKL = [1  1           0;
                1 -bg[1]/bg[2] 0;
               -1 -1 sum(bg[1:2])/bg[3]]
    if isnorm
        # Each column of the inverse of dLMSToDKL is the differential LMS relative to bg that isolating each DKL mechanism
        lms_dkliso = inv(dLMSToDKL)
        # Cone contrast relative to bg
        cc = lms_dkliso./bg
        # Pooled cone contrast of each differential LMS vector that isolating each DKL mechanism
        pcc = [norm(cc[:,i]) for i in 1:3]
        # Scale differential LMS vector by its pooled cone contrast
        ulms_dkliso = lms_dkliso./pcc'
        # Rescale dLMSToDKL so that differential LMS that isolating DKL mechanism and having unit pooled cone contrast will result unit DKL response
        dLMSToDKL = inv(dLMSToDKL*ulms_dkliso)*dLMSToDKL
    end
    DKLTodLMS = inv(dLMSToDKL)
    return dLMSToDKL,DKLTodLMS
end


# Human Cone Fundamentals from http://www.cvrl.org/cones.htm
# 2-deg fundamentals based on the Stiles & Burch 10-deg CMFs (adjusted to 2-deg), Stockman & Sharpe (2000), Linear Energy, 0.1nm
conef = "linss2_10e"
# 10-deg fundamentals based on the Stiles & Burch 10-deg CMFs, Stockman & Sharpe (2000), Linear Energy, 0.1nm
conef = "linss10e"

# Cone Fundamentals Prepared with Missing replaced to 0
linss10e = mapcols(x->coalesce.(x,0.0), CSV.read("$(conef)_fine.csv",header=[:r,:L,:M,:S]))
CSV.write("$(conef).csv",linss10e)

@df linss10e Plots.plot(:r,[:L,:M,:S],color=["Red" "Green" "Blue"],xlabel="Wavelength (nm)",ylabel="Sensitivity",label=["L" "M" "S"],title="Cone Fundamentals($conef)")
foreach(i->savefig("Cone Fundamentals($conef)$i"),[".png",".svg"])



# ViewSonic VX3276mhd IPS LCD
displayname = "VX3276mhd"
# Sony Trinitron CRT
displayname = "Trinitron"
resultdir = "./$displayname"
mkpath(resultdir)
config = YAML.load_file("CommandConfig_SNL-C.yaml")
RGBToLMS,LMSToRGB = RGBLMSMatrix(RGBSpectral(config["Display"][displayname]["SpectralMeasurement"])...,linss10e)


# Transformation between specific display RGB and LMS Spaces
ucr=0:0.01:1
ucs=[[i,j,k] for i=ucr,j=ucr,k=ucr][:]
ucm=hcat(ucs...)
lms_rgb = RGBToLMS*ucm
rgb_lms = LMSToRGB*ucm

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in ucs],markersize=0.01,transparency=true)
s.center=false
Makie.save("Unit Color Space.png",s)

record(s,"Unit Color Space.mp4",1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end


Makie.scatter!(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,1) for i in ucs],markersize=0.01,transparency=true)
s[Axis][:names,:axisnames]=("R / L","G / M","B / S")
Makie.save(joinpath(resultdir,"RGB To LMS Space.png"),s)

record(s,joinpath(resultdir,"RGB To LMS Space.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,1) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(rgb_lms[1,:],rgb_lms[2,:],rgb_lms[3,:],color=[RGBA(i...,1) for i in ucs],markersize=0.01,transparency=true)
s[Axis][:names,:axisnames]=("R / L","G / M","B / S")
Makie.save(joinpath(resultdir,"LMS To RGB Space.png"),s)

record(s,joinpath(resultdir,"LMS To RGB Space.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

# Cone Isolating Modulation Range
midlms= RGBToLMS*[0.5,0.5,0.5]
histogram2d(lms_rgb[2,:],lms_rgb[3,:],nbins=60,color=:reds,title="Liso Strength",xlabel="M",ylabel="S")
vline!(midlms[2:2],color=:gray);hline!(midlms[3:3],color=:gray,legend=false)
foreach(i->savefig(joinpath(resultdir,"Liso Strength$i")),[".png",".svg"])
histogram2d(lms_rgb[1,:],lms_rgb[3,:],nbins=60,color=:greens,title="Miso Strength",xlabel="L",ylabel="S")
vline!(midlms[1:1],color=:gray);hline!(midlms[3:3],color=:gray,legend=false)
foreach(i->savefig(joinpath(resultdir,"Miso Strength$i")),[".png",".svg"])
histogram2d(lms_rgb[1,:],lms_rgb[2,:],nbins=60,color=:blues,title="Siso Strength",xlabel="L",ylabel="M")
vline!(midlms[1:1],color=:gray);hline!(midlms[2:2],color=:gray,legend=false)
foreach(i->savefig(joinpath(resultdir,"Siso Strength$i")),[".png",".svg"])

# Cone Isolating RGB Color through Gray [0.5, 0.5, 0.5]
ConeIsoRGB = LMSToRGB*[1 0 0;
                       0 1 0;
                       0 0 1]
# Scale Each Cone Isolating Vector into Unit Cube
ConeIsoRGB./=maximum(abs.(ConeIsoRGB),dims=1)

mr = -0.5:0.001:0.5
Lisos = hcat([collect(mr*ConeIsoRGB[i,1].+0.5) for i=1:3]...)'
Lisosc=[RGB(Lisos[:,i]...) for i in 1:size(Lisos,2)]
Lisoslms=RGBToLMS*Lisos

Misos = hcat([collect(mr*ConeIsoRGB[i,2].+0.5) for i=1:3]...)'
Misosc=[RGB(Misos[:,i]...) for i in 1:size(Misos,2)]
Misoslms=RGBToLMS*Misos

Sisos = hcat([collect(mr*ConeIsoRGB[i,3].+0.5) for i=1:3]...)'
Sisosc = [RGB(Sisos[:,i]...) for i in 1:size(Sisos,2)]
Sisoslms=RGBToLMS*Sisos

s=Makie.scatter(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,0.01) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(Lisoslms[1,:],Lisoslms[2,:],Lisoslms[3,:],color=Lisosc,markersize=0.01,transparency=true)
Makie.scatter!(Misoslms[1,:],Misoslms[2,:],Misoslms[3,:],color=Misosc,markersize=0.01,transparency=true)
Makie.scatter!(Sisoslms[1,:],Sisoslms[2,:],Sisoslms[3,:],color=Sisosc,markersize=0.01,transparency=true)
s[Axis][:names,:axisnames]=("L","M","S")
s.center=false
Makie.save(joinpath(resultdir,"ConeIsolating_ThroughGray_LMSSpace.png"),s)

record(s,joinpath(resultdir,"ConeIsolating_ThroughGray_LMSSpace.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,0.01) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(Lisos[1,:],Lisos[2,:],Lisos[3,:],color=Lisosc,markersize=0.01,transparency=true)
Makie.scatter!(Misos[1,:],Misos[2,:],Misos[3,:],color=Misosc,markersize=0.01,transparency=true)
Makie.scatter!(Sisos[1,:],Sisos[2,:],Sisos[3,:],color=Sisosc,markersize=0.01,transparency=true)
s[Axis][:names,:axisnames]=("R","G","B")
s.center=false
Makie.save(joinpath(resultdir,"ConeIsolating_ThroughGray_RGBSpace.png"),s)

record(s,joinpath(resultdir,"ConeIsolating_ThroughGray_RGBSpace.mp4"),1:360/5,framerate=12) do i
    rotate_cam!(s,5pi/180,0.0,0.0)
end

# Transformation between LMS and Cone Contrast relative to bg
bg = [0.5,0.5,0.5]
LMSToContrast,ContrastToLMS = LMSContrastMatrix(bg)
cc_lms = LMSToContrast*vcat(ucm,ones(size(ucm,2))')
lms_cc = ContrastToLMS*vcat(ucm,ones(size(ucm,2))')

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(cc_lms[1,:],cc_lms[2,:],cc_lms[3,:],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
Makie.save("LMS To Cone Contrast$bg Space.png",s)

s=Makie.scatter(ucm[1,:],ucm[2,:],ucm[3,:],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(lms_cc[1,:],lms_cc[2,:],lms_cc[3,:],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
Makie.save("Cone Contrast$bg To LMS Space.png",s)

bg = RGBToLMS*[0.5,0.5,0.5]
LMSToContrast,ContrastToLMS = LMSContrastMatrix(bg)
cc_lms = LMSToContrast*vcat(lms_rgb,ones(size(lms_rgb,2))')

s=Makie.scatter(lms_rgb[1,:],lms_rgb[2,:],lms_rgb[3,:],color=[RGBA(i...,0.01) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(cc_lms[1,:],cc_lms[2,:],cc_lms[3,:],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
s.center=false
Makie.save(joinpath(resultdir,"LMS To Cone Contrast$bg Space.png"),s)


# Transformation between differential LMS and DKL Spaces
bg = [0.5,0.5,0.5]
dLMSToDKL,DKLTodLMS = dLMSDKLMatrix(bg,isnorm=true)
dkl_lms = dLMSToDKL*(ucm.-bg)
lms_dkl = DKLTodLMS*ucm

s=Makie.scatter(ucm[1,:].-bg[1],ucm[2,:].-bg[2],ucm[3,:].-bg[3],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(dkl_lms[1,:],dkl_lms[2,:],dkl_lms[3,:],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
s.center=false
Makie.save("dLMS To DKL$bg Space.png",s)

s=Makie.scatter(ucm[1,:].-bg[1],ucm[2,:].-bg[2],ucm[3,:].-bg[3],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(lms_dkl[1,:],lms_dkl[2,:],lms_dkl[3,:],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
s.center=false
Makie.save("DKL$bg To dLMS Space.png",s)

bg = RGBToLMS*[0.5,0.5,0.5]
dLMSToDKL,DKLTodLMS = dLMSDKLMatrix(bg,isnorm=true)
dkl_lms = dLMSToDKL*(lms_rgb.-bg)

s=Makie.scatter(lms_rgb[1,:].-bg[1],lms_rgb[2,:].-bg[2],lms_rgb[3,:].-bg[3],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
Makie.scatter!(dkl_lms[1,:],dkl_lms[2,:],dkl_lms[3,:],color=[RGBA(i...,0.1) for i in ucs],markersize=0.01,transparency=true)
s[Axis][:names,:axisnames]=("dL / (L+M)","dM / (L-M)","dS / (S-(L+M))")
s.center=false
Makie.save(joinpath(resultdir,"dLMS To DKL$bg Space.png"),s)
