using YAML,PyCall,Plots
include("color_algorithm.jl")

"Get digital RGB color spectral measured from a specific display"
function RGBSpectral(measurement)
    C = map(i->parse.(Float64,split(i)),measurement["Color"])
    λ = measurement["WL"]
    I = measurement["Spectral"]
    return C,λ,I
end

colorstring(c) = join(string.(c)," ")


# Generate all color related data for a display
displayname = "ROGPG279Q" # "Trinitron"
resultdir = "../Data/$displayname"
mkpath(resultdir)
resultpath = "$resultdir/colordata.yaml"

config = YAML.load_file("../Configuration/CommandConfig_SNL-C.yaml")
RGBToLMS,LMSToRGB = RGBLMSMatrix(RGBSpectral(config["Display"][displayname]["SpectralMeasurement"])...)
RGBToXYZ,XYZToRGB = RGBXYZMatrix(RGBSpectral(config["Display"][displayname]["SpectralMeasurement"])...)


# Maximum Cone Isolating RGB through a color
th = [0.5,0.5,0.5]
# each column of LMSToRGB is the cone isolating RGB direction
ConeIsoRGBVec = trimatrix(LMSToRGB)
# Scale RGB Vector into Unit Cube
ConeIsoRGBVec./=maximum(abs.(ConeIsoRGBVec),dims=1)
# since through color is the center of RGB cube, the line intersects at two symmatric points on the faces of unit cube
minc_lms = quavectors(th.-0.5*ConeIsoRGBVec);maxc_lms = quavectors(th.+0.5*ConeIsoRGBVec)
mcc = contrast_michelson.(RGBToLMS*maxc_lms,RGBToLMS*minc_lms)

# Cone Isolating RGB color with same pooled michelson cone contrast
# since all the maximum Cone Isolating color pairs are symmatric around `th` color, i.e. `th = (maxc+minc)/2`, then the weber contrast
# using `maxc and th` is equivalent to michelson contrast using `maxc and minc`. here use Weber Cone Contrast to scale to the minimum michelson contrast.
LMSToContrast,ContrastToLMS = LMSContrastMatrix(RGBToLMS*[th;1])
maxcc = LMSToContrast*RGBToLMS*maxc_lms
mincc = LMSToContrast*RGBToLMS*minc_lms
pcc = [norm(maxcc[1:3,i]) for i in 1:3]
ccmf = minimum(pcc)./pcc
ccmf[3]=1 # don't scale S Cone
mmaxc_lms = LMSToRGB*ContrastToLMS*quavectors(ccmf'.*trivectors(maxcc))
mminc_lms = LMSToRGB*ContrastToLMS*quavectors(ccmf'.*trivectors(mincc))
mmcc = contrast_michelson.(RGBToLMS*mmaxc_lms,RGBToLMS*mminc_lms)


# DKL Isolating RGB through a background color
bg = RGBToLMS*[th;1]
LMSToDKL,DKLToLMS = LMSDKLMatrix(bg,isnorm=true)
# convert each column of DKLToLMS which are the DKL isolating LMS to RGB direction
DKLIsoRGBVec = trimatrix(LMSToRGB*DKLToLMS)
# Scale RGB Vector into Unit Cube
DKLIsoRGBVec./=maximum(abs.(DKLIsoRGBVec),dims=1)
# since through color is the center of RGB cube, the line intersects at two symmatric points on the faces of unit cube
minc_dkl = quavectors(th.-0.5*DKLIsoRGBVec);maxc_dkl = quavectors(th.+0.5*DKLIsoRGBVec)


# DKL Isoluminance plane
DKLToRGB = LMSToRGB*DKLToLMS
RGBToDKL = LMSToDKL*RGBToLMS
lum = 0;hueangle = 30
hueangles = [0,30,60,75,80,83,90,95,101,105,110,120,150]
hueangles = 0:hueangle:180-hueangle
# rotate l-m direction around l+m axis within the Isoluminance plane
DKLIsoLumRGBVec = trivectors(hcat(map(i->DKLToRGB*RotateXYZMatrix(deg2rad(i),dims=1)*[0,1,0,0], hueangles)...))
# find Intersections of Isoluminance directions with faces of unit RGB cube
minmaxc = hcat(map(i->intersectlineunitorigincube((DKLToRGB*[lum,0,0,1])[1:3],DKLIsoLumRGBVec[:,i]),1:size(DKLIsoLumRGBVec,2))...)
minc_dkl_ilp=minmaxc[:,1:2:end];maxc_dkl_ilp=minmaxc[:,2:2:end]
hue_dkl_ilp = quavectors([maxc_dkl_ilp minc_dkl_ilp])
wp_dkl_ilp = repeat([th;1],inner=(1,size(hue_dkl_ilp,2)))

# Check Hues
IsoLumc = [RGB(hue_dkl_ilp[1:3,i]...) for i in 1:size(hue_dkl_ilp,2)]
IsoLumdkl = RGBToDKL*hue_dkl_ilp
p=plot(IsoLumdkl[2,:],IsoLumdkl[3,:],aspectratio=:equal,color=IsoLumc,lw=1.5,markersize=4.5,marker=:circle,markerstrokewidth=0,legend=false,xlabel="L-M",ylabel="S-(L+M)")
foreach(i->plot!(p,[0,IsoLumdkl[2,i]],[0,IsoLumdkl[3,i]],color=:gray,linestyle=:dot),1:size(hue_dkl_ilp,2))
p


# HSL equal angular distance hue[0:30:330] and equal energy white with matched luminance
hslhuewp = copy(hslhuewpYmatch)
hslhuewp[3,:].= 0.4
huewp_hsl=XYZToRGB*quavectors(xyY2XYZ(hslhuewp))
huewphslc = [RGB(huewp_hsl[1:3,i]...) for i in 1:size(huewp_hsl,2)]
hue_hsl = huewp_hsl[:,1:end-1]
wp_hsl = repeat(huewp_hsl[:,end],inner=(1,size(hue_hsl,2)))









using Colors

a=[HSV(i,1,1) for i in 0:30:360]

b=cat(map(i->[i.r,i.g,i.b],RGB.(a))...,dims=2)

c=RGBToXYZ*quavectors(b)

plotlyjs()


XYZ2xyY(c)




hslhue = copy(hslhue_Ymatch)
hslhue[3,:].=0.35
hue_hsl_Ymatch=XYZToRGB*quavectors(xyY2XYZ(hslhue))



[RGB(a[:,i]...) for i in 1:size(a,2)]
a./=maximum(abs.(a),dims=1)

RGBToXYZ*[0,0,1,1]



RGBToLMS*[th;1]

# Save color data
colordata = Dict("LMS_X"=>colorstring.([maxc_lms[:,1],minc_lms[:,1]]),
                "LMS_Y"=>colorstring.([maxc_lms[:,2],minc_lms[:,2]]),
                "LMS_Z"=>colorstring.([maxc_lms[:,3],minc_lms[:,3]]),
                #"LMS_XYZ_MichelsonContrast" => colorstring(diag(mcc)),
                "LMS_Xm"=>colorstring.([mmaxc_lms[:,1],mminc_lms[:,1]]),
                "LMS_Ym"=>colorstring.([mmaxc_lms[:,2],mminc_lms[:,2]]),
                "LMS_Zm"=>colorstring.([mmaxc_lms[:,3],mminc_lms[:,3]]),
                #"LMS_XYZm_MichelsonContrast" => colorstring(diag(mmcc)),
                "DKL_X"=>colorstring.([maxc_dkl[:,1],minc_dkl[:,1]]),
                "DKL_Y"=>colorstring.([maxc_dkl[:,2],minc_dkl[:,2]]),
                "DKL_Z"=>colorstring.([maxc_dkl[:,3],minc_dkl[:,3]]),
                "HSL_HueYmatch" => colorstring.([hue_hsl_Ymatch[:,i] for i in 1:size(hue_hsl_Ymatch,2)]),
                "HSL_WPYmatch" => colorstring.([wp_hsl[:,i] for i in 1:size(wp_hsl,2)]),
                "DKL_HueMax_0"=>colorstring.([hue_dkl_ilp[:,i] for i in 1:size(hue_dkl_ilp,2)]),
                "DKL_WP_0"=>colorstring.([wp_dkl_ilp[:,i] for i in 1:size(wp_dkl_ilp,2)]))

pyyaml = pyimport("yaml")
open(resultpath,"w") do f
    pyyaml.dump(colordata,f)
end








using MAT
a=matread("hueTest.mat")
a=a["test1"]

IsoLumc = [RGB(a[1:3,i]...) for i in 1:size(a,2)]




IsoLumc = [RGB(a[1:3,i]...) for i in 1:size(a,2)]
IsoLumdkl = RGBToDKL*quavectors(a)
p=plot(IsoLumdkl[2,:],IsoLumdkl[3,:],aspectratio=:equal,color=IsoLumc,lw=1.5,markersize=4.5,marker=:circle,markerstrokewidth=0,legend=false,xlabel="L-M",ylabel="S-(L+M)")
foreach(i->plot!(p,[0,IsoLumdkl[2,i]],[0,IsoLumdkl[3,i]],color=:gray,linestyle=:dot),1:size(hue_dkl_ilp,2))
p
