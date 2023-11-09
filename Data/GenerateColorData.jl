# Generate all color data for a display
using ColorLab,LinearAlgebra,YAML,Plots

"Get RGB color spectra measured from a specific display"
function RGBSpectra(measurement)
    C = map(i->parse.(Float64,split(i)),measurement["Color"])
    λ = measurement["WL"]
    I = measurement["Spectral"]
    return C,λ,I
end
colorstring(c::AbstractVector) = join(string.(c)," ")


## Transformation of color spaces
displayname = "ROGPG279Q"
resultdir = joinpath(@__DIR__,displayname);mkpath(resultdir)
colordatapath = joinpath(resultdir,"colordata.yaml")
figfmt = [".png",".svg"]

configname = "CommandConfig_SNL-C"
config = YAML.load_file(joinpath(splitdir(@__DIR__)[1],"Configuration","$configname.yaml"))
Cs = RGBSpectra(config["Display"][displayname]["SpectralMeasurement"])
observer = 10
RGBToLMS,LMSToRGB = RGBLMSMatrix(Cs...;observer)
RGBToXYZ,XYZToRGB = RGBXYZMatrix(Cs...;observer)


## Maximum Cone Isolating RGBs through a background color
th = [0.5,0.5,0.5]
# Each column of LMSToRGB is the cone isolating RGB direction
ConeIsoRGBVec = dehomomatrix(LMSToRGB)
# Scale RGB direction into Unit Cube
ConeIsoRGBVec./=maximum(abs.(ConeIsoRGBVec),dims=1)
# Since `th` color is the center of RGB cube, the line intersects at two symmatric points on the faces of unit cube
minrgb_lmsiso = clamp.(homovector(th .- 0.5ConeIsoRGBVec),0,1);maxrgb_lmsiso = clamp.(homovector(th .+ 0.5ConeIsoRGBVec),0,1)
mcc = contrast_michelson.(RGBToLMS*maxrgb_lmsiso,RGBToLMS*minrgb_lmsiso)


## Cone Isolating RGBs with same pooled michelson cone contrast
# Since all the maximum Cone Isolating color pairs are symmatric around `th` color, i.e. `th = (max+min)/2`,
# then the Weber Contrast using `max and th` is equivalent to Michelson Contrast using `max and min`.
# Here we use Weber Cone Contrast to scale to the minimum Michelson Contrast.
bgrgb = homovector(th)
bglms = RGBToLMS*bgrgb
LMSToContrast,ContrastToLMS = LMSContrastMatrix(bglms)
maxcc = dehomovector(LMSToContrast*RGBToLMS*maxrgb_lmsiso)
mincc = dehomovector(LMSToContrast*RGBToLMS*minrgb_lmsiso)
pcc = [norm(i) for i in eachslice(maxcc,dims=2)]
ccf = minimum(pcc)./pcc
# Since S cone contrast is much larger than L and M, scale S to L and M will significently decrease effectiveness
# of the S Cone Isolating color, so here we don't scale S Cone
ccf[3]=1
mmaxrgb_lmsiso = clamp.(LMSToRGB*ContrastToLMS*homovector(ccf'.*maxcc),0,1)
mminrgb_lmsiso = clamp.(LMSToRGB*ContrastToLMS*homovector(ccf'.*mincc),0,1)
mmcc = contrast_michelson.(RGBToLMS*mmaxrgb_lmsiso,RGBToLMS*mminrgb_lmsiso)


## DKL Isolating RGBs through a background color
LMSToDKL,DKLToLMS = LMSDKLMatrix(bglms,isnorm=true)
# Each column of DKLToLMS is the DKL Isolating LMS direction, then it's converted to RGB direction
DKLIsoRGBVec = dehomomatrix(LMSToRGB*DKLToLMS)
# Scale RGB direction into Unit Cube
DKLIsoRGBVec./=maximum(abs.(DKLIsoRGBVec),dims=1)
# Since `th` color is the center of RGB cube, the line intersects at two symmatric points on the faces of unit cube
minrgb_dkliso = clamp.(homovector(th .- 0.5DKLIsoRGBVec),0,1);maxrgb_dkliso = clamp.(homovector(th .+ 0.5DKLIsoRGBVec),0,1)


## DKL Isoluminance plane
DKLToRGB = LMSToRGB*DKLToLMS
RGBToDKL = LMSToDKL*RGBToLMS
lum = 0
lumrgb = dehomovector(DKLToRGB*[lum,0,0,1])
hueangle_dkl_ilp = [0,30,60,75,80,83,90,95,101,105,110,120,150]
append!(hueangle_dkl_ilp,hueangle_dkl_ilp .+ 180)
# Rotate `L+` direction around `Lum` axis within the Isoluminance Plane
DKLIsoLumRGBVec = dehomovector(stack(i->DKLToRGB*RotateXMatrix(deg2rad(i))*[0,1,0,0], hueangle_dkl_ilp))
# Find Intersections of Isoluminance directions with faces of unit RGB cube
anglec = stack(i->intersectlineunitcube(lumrgb,i),eachslice(DKLIsoLumRGBVec,dims=2))
hue_dkl_ilp = clamp.(homovector(anglec),0,1)
wp_dkl_ilp = repeat(bgrgb,inner=(1,size(hue_dkl_ilp,2)))

# Plot DKL Hues
IsoLumcolor = [RGB(hue_dkl_ilp[1:3,i]...) for i in 1:size(hue_dkl_ilp,2)]
IsoLumdkl = RGBToDKL*hue_dkl_ilp

title="DKL_$(length(hueangle_dkl_ilp))Hue_L$lum"
p=plot(size=(400,400))
foreach(i->plot!(p,[0,IsoLumdkl[2,i]],[0,IsoLumdkl[3,i]],color=RGBA(0.5,0.5,0.5,0.5)),1:size(hue_dkl_ilp,2))
plot!(p,IsoLumdkl[2,:],IsoLumdkl[3,:];ratio=:equal,color=IsoLumcolor,lw=1.5,markersize=5,marker=:circle,tickdir=:out,
markerstrokewidth=0,legend=false,xlabel="L-M",ylabel="S-(L+M)",title)
p
foreach(ext->savefig(joinpath(resultdir,"$title$ext")),figfmt)


## HSL equal angular distance hue[0:30:330] and equal energy white with matched luminance in CIE [x,y,Y] coordinates
hueangle_hsl = 0:30:330
# Name:     R:1             Y:3             G:5                             B:9                             WP
hslhues =  [0.63    0.54    0.42    0.34    0.3     0.27    0.22    0.17    0.15    0.2     0.32    0.5     0.33;
            0.34    0.41    0.5     0.57    0.6     0.5     0.33    0.15    0.07    0.1     0.16    0.27    0.33;
            1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0]

# Computed RGBAs
hslhues[3,:] .= 14.5
hues_hsl = clamp.(XYZToRGB*xyY2XYZ(hslhues),0,1)

# Mannual adjusted and tested RGBAs
hues_xyY = [0.6345  0.5461  0.4214  0.3419  0.3074  0.2738  0.2251  0.1753  0.1529  0.2052  0.3211  0.5086  0.3294;
            0.3458  0.4147  0.5152  0.5714  0.6035  0.5074  0.3332  0.1538  0.0729  0.1103  0.1653  0.2724  0.3292;
            14.410  14.540  14.520  14.300  14.430  14.340  14.480  14.360  14.540  14.400  14.490  14.480  14.45]

hues_hsl = [0.370   0.225   0.087   0.028   0.006   0.000   0.000   0.000   0.000   0.120   0.280   0.350   0.098;
            0.010   0.051   0.089   0.108   0.115   0.113   0.103   0.078   0.028   0.030   0.012   0.010   0.078;
            0.000   0.000   0.001   0.003   0.001   0.030   0.110   0.410   1.000   0.550   0.290   0.067   0.083;
            1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0     1.0]

hue_hsl = hues_hsl[:,1:end-1]
wp_hsl = repeat(hues_hsl[:,end],inner=(1,size(hue_hsl,2)))

# Plot HSL Hues
IsoLumcolor = [RGB(hue_hsl[1:3,i]...) for i in 1:size(hue_hsl,2)]
IsoLumhsl = [cos.(deg2rad.(hueangle_hsl)) sin.(deg2rad.(hueangle_hsl))]'

title="HSL_$(length(hueangle_hsl))Hue_Ym"
p=plot(size=(400,400))
foreach(i->plot!(p,[0,IsoLumhsl[1,i]],[0,IsoLumhsl[2,i]],color=RGBA(0.5,0.5,0.5,0.5)),1:size(hue_hsl,2))
plot!(p,IsoLumhsl[1,:],IsoLumhsl[2,:];ratio=:equal,color=IsoLumcolor,lw=1.5,markersize=8,marker=:circle,tickdir=:out,
markerstrokewidth=0,legend=false,xlabel="S",ylabel="S",title)
p
foreach(ext->savefig(joinpath(resultdir,"$title$ext")),figfmt)


## Save color data
colordata = Dict{String,Any}("LMS_X"=>colorstring.([minrgb_lmsiso[:,1],maxrgb_lmsiso[:,1]]),
                            "LMS_Y"=>colorstring.([minrgb_lmsiso[:,2],maxrgb_lmsiso[:,2]]),
                            "LMS_Z"=>colorstring.([minrgb_lmsiso[:,3],maxrgb_lmsiso[:,3]]),
                            "LMS_X_WP"=>colorstring.([bgrgb,bgrgb]),
                            "LMS_Y_WP"=>colorstring.([bgrgb,bgrgb]),
                            "LMS_Z_WP"=>colorstring.([bgrgb,bgrgb]),
                            "LMS_XYZ_MichelsonContrast" => diag(mcc),
                            "LMS_Xmcc"=>colorstring.([mminrgb_lmsiso[:,1],mmaxrgb_lmsiso[:,1]]),
                            "LMS_Ymcc"=>colorstring.([mminrgb_lmsiso[:,2],mmaxrgb_lmsiso[:,2]]),
                            "LMS_Zmcc"=>colorstring.([mminrgb_lmsiso[:,3],mmaxrgb_lmsiso[:,3]]),
                            "LMS_Xmcc_WP"=>colorstring.([bgrgb,bgrgb]),
                            "LMS_Ymcc_WP"=>colorstring.([bgrgb,bgrgb]),
                            "LMS_Zmcc_WP"=>colorstring.([bgrgb,bgrgb]),
                            "LMS_XYZmcc_MichelsonContrast" => diag(mmcc),
                            "DKL_X"=>colorstring.([minrgb_dkliso[:,1],maxrgb_dkliso[:,1]]),
                            "DKL_Y"=>colorstring.([minrgb_dkliso[:,2],maxrgb_dkliso[:,2]]),
                            "DKL_Z"=>colorstring.([minrgb_dkliso[:,3],maxrgb_dkliso[:,3]]),
                            "DKL_X_WP"=>colorstring.([bgrgb,bgrgb]),
                            "DKL_Y_WP"=>colorstring.([bgrgb,bgrgb]),
                            "DKL_Z_WP"=>colorstring.([bgrgb,bgrgb]),
                            "DKL_HueL0_Angle"=>hueangle_dkl_ilp,
                            "DKL_HueL0"=>colorstring.(eachslice(hue_dkl_ilp,dims=2)),
                            "DKL_HueL0_WP"=>colorstring.(eachslice(wp_dkl_ilp,dims=2)),
                            "HSL_HueYm_Angle" => hueangle_hsl,
                            "HSL_HueYm" => colorstring.(eachslice(hue_hsl,dims=2)),
                            "HSL_HueYm_WP" => colorstring.(eachslice(wp_hsl,dims=2)),
                            "HSL_RGYm" => colorstring.([hue_hsl[:,i] for i in (1,5)]),
                            "HSL_RGYm_WP" => colorstring.([wp_hsl[:,i] for i in (1,5)]),
                            "HSL_YBYm" => colorstring.([hue_hsl[:,i] for i in (3,9)]),
                            "HSL_YBYm_WP" => colorstring.([wp_hsl[:,i] for i in (3,9)]),
                            "HSL_RBYm" => colorstring.([hue_hsl[:,i] for i in (1,9)]),
                            "HSL_RBYm_WP" => colorstring.([wp_hsl[:,i] for i in (1,9)]),
                            "HSL_YGYm" => colorstring.([hue_hsl[:,i] for i in (3,5)]),
                            "HSL_YGYm_WP" => colorstring.([wp_hsl[:,i] for i in (3,5)]),
                            "RGBToLMS" => vec(RGBToLMS),
                            "LMSToRGB" => vec(LMSToRGB),
                            "RGBToXYZ" => vec(RGBToXYZ),
                            "XYZToRGB" => vec(XYZToRGB),
                            "LMSToContrast" => vec(LMSToContrast),
                            "ContrastToLMS" => vec(ContrastToLMS),
                            "LMSToDKL" => vec(LMSToDKL),
                            "DKLToLMS" => vec(DKLToLMS),
                            "DKLToRGB" => vec(DKLToRGB),
                            "RGBToDKL" => vec(RGBToDKL))
YAML.write_file(colordatapath,colordata)

