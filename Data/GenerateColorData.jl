using ColorLab,LinearAlgebra,YAML,Plots

"Get digital RGB color spectral measured from a specific display"
function RGBSpectral(measurement)
    C = map(i->parse.(Float64,split(i)),measurement["Color"])
    λ = measurement["WL"]
    I = measurement["Spectral"]
    return C,λ,I
end
colorstring(c::Vector) = join(string.(c)," ")

## Generate all color related data for a display
cd(@__DIR__)
displayname = "ROGPG279Q"
resultdir = "./$displayname"
isdir(resultdir) || mkpath(resultdir)
colordatapath = joinpath(resultdir,"colordata.yaml")
figfmt = [".png",".svg"]

config = YAML.load_file("../Configuration/CommandConfig_SNL-C.yaml")
RGBToLMS,LMSToRGB = RGBLMSMatrix(RGBSpectral(config["Display"][displayname]["SpectralMeasurement"])...)
RGBToXYZ,XYZToRGB = RGBXYZMatrix(RGBSpectral(config["Display"][displayname]["SpectralMeasurement"])...)







a=rand(3,10)
a_xyz = trivectors(RGBToXYZ*quavectors(a))

a_xyz = [19.01	57.06	3.53	19.01;
        20.00	43.06	6.56	20.00;
        21.78   31.96   2.14    21.78]

camview = cam16view(W=[100,100.00,100],La=200.31,Yb=20)

cam = XYZ2CAM16(a_xyz;camview...)
ucs = CAM16UCS(cam.J,cam.M,cam.h)



a=[1,2,3]

string(a)

rcam = CAM16UCSinv(ucs)

rxyz = CAM162XYZ(cam.J,cam.M,cam.h;camview...)



rxyz = CAM162XYZ(rcam...;camview...)


cam.J

rad2deg(cam.h[2])


RGBToXYZ*[0.5,0.5,0.5,1]


cam16uhs = hcat([[j,m,h] for h in deg2rad.(cam16uniquehue[1,1:end-1]), m in 25:25, j in 15:15]...)
#rcam = CAM16UCSinv(cam16uhs,form=:polar)
#uhs_rgb = XYZToRGB*quavectors(CAM162XYZ(rcam...;camview...))
uhs_rgb = XYZToRGB*quavectors(CAM162XYZ(cam16uhs[1,:],cam16uhs[2,:],cam16uhs[3,:];camview...))


uhs_rgb = quavectors(desaturate2gamut!(trivectors(uhs_rgb)))
IsoLumc = [RGB(uhs_rgb[1:3,i]...) for i in 1:size(uhs_rgb,2)]
IsoLumdkl = RGBToDKL*uhs_rgb
p=plot(IsoLumdkl[2,:],IsoLumdkl[3,:],aspectratio=:equal,color=IsoLumc,lw=1.5,markersize=4.5,marker=:circle,markerstrokewidth=0,legend=false,xlabel="L-M",ylabel="S-(L+M)")
foreach(i->plot!(p,[0,IsoLumdkl[2,i]],[0,IsoLumdkl[3,i]],color=:gray,linestyle=:dot),1:size(hue_dkl_ilp,2))
p









## Maximum Cone Isolating RGBs through a RGB color
th = [0.5,0.5,0.5]
# each column of LMSToRGB is the cone isolating RGB direction
ConeIsoRGBVec = trimatrix(LMSToRGB)
# Scale RGB direction into Unit Cube
ConeIsoRGBVec./=maximum(abs.(ConeIsoRGBVec),dims=1)
# since through color is the center of RGB cube, the line intersects at two symmatric points on the faces of unit cube
minc_lms = clamp.(quavectors(th .- 0.5ConeIsoRGBVec),0,1);maxc_lms = clamp.(quavectors(th .+ 0.5ConeIsoRGBVec),0,1)
mcc = contrast_michelson.(RGBToLMS*maxc_lms,RGBToLMS*minc_lms)

## Cone Isolating RGBs with same pooled michelson cone contrast
# since all the maximum Cone Isolating color pairs are symmatric around `th` color, i.e. `th = (maxc+minc)/2`,
# then the weber contrast using `maxc and th` is equivalent to michelson contrast using `maxc and minc`.
# Here we use Weber Cone Contrast to scale to the minimum michelson contrast.
LMSToContrast,ContrastToLMS = LMSContrastMatrix(RGBToLMS*[th;1])
maxcc = LMSToContrast*RGBToLMS*maxc_lms
mincc = LMSToContrast*RGBToLMS*minc_lms
pcc = [norm(maxcc[1:3,i]) for i in 1:3]
ccf = minimum(pcc)./pcc
# since S cone contrast is much larger than L and M, scale S to L and M will significently decrease effectiveness
# of the S Cone Isolating color, so here we don't scale S Cone
ccf[3]=1
mmaxc_lms = clamp.(LMSToRGB*ContrastToLMS*quavectors(ccf'.*trivectors(maxcc)),0,1)
mminc_lms = clamp.(LMSToRGB*ContrastToLMS*quavectors(ccf'.*trivectors(mincc)),0,1)
mmcc = contrast_michelson.(RGBToLMS*mmaxc_lms,RGBToLMS*mminc_lms)

## DKL Isolating RGBs through a background RGB color
bg = RGBToLMS*[th;1]
LMSToDKL,DKLToLMS = LMSDKLMatrix(bg,isnorm=true)
# each column of DKLToLMS is the DKL Isolating LMS direction, then it's converted to RGB direction
DKLIsoRGBVec = trimatrix(LMSToRGB*DKLToLMS)
# Scale RGB direction into Unit Cube
DKLIsoRGBVec./=maximum(abs.(DKLIsoRGBVec),dims=1)
# since through color is the center of RGB cube, the line intersects at two symmatric points on the faces of unit cube
minc_dkl = clamp.(quavectors(th .- 0.5DKLIsoRGBVec),0,1);maxc_dkl = clamp.(quavectors(th .+ 0.5DKLIsoRGBVec),0,1)

## DKL Isoluminance plane
DKLToRGB = LMSToRGB*DKLToLMS
RGBToDKL = LMSToDKL*RGBToLMS
lum = 0
huestep = 1;hueangles = 0:huestep:180-huestep
# hueangles = [0,30,60,75,80,83,90,95,101,105,110,120,150]
# rotate l+ direction around l+m axis within the `lum` Isoluminance plane
DKLIsoLumRGBVec = trivectors(hcat(map(i->DKLToRGB*RotateXYZMatrix(deg2rad(i),dims=1)*[0,1,0,0], hueangles)...))
# find Intersections of Isoluminance directions with faces of unit RGB cube
minmaxc = hcat(map(i->intersectlineunitorigincube((DKLToRGB*[lum,0,0,1])[1:3],DKLIsoLumRGBVec[:,i]),1:size(DKLIsoLumRGBVec,2))...)
minc_dkl_ilp=minmaxc[:,1:2:end];maxc_dkl_ilp=minmaxc[:,2:2:end]
hue_dkl_ilp = clamp.(quavectors([maxc_dkl_ilp minc_dkl_ilp]),0,1)
wp_dkl_ilp = repeat([th;1],inner=(1,size(hue_dkl_ilp,2)))
hueangle_dkl_ilp = [hueangles;hueangles.+180]

## Plot DKL Hues
IsoLumc = [RGB(hue_dkl_ilp[1:3,i]...) for i in 1:size(hue_dkl_ilp,2)]
IsoLumdkl = RGBToDKL*hue_dkl_ilp

title="DKL_$(length(hueangle_dkl_ilp))Hue_L$lum"
p=plot()
foreach(i->plot!(p,[0,IsoLumdkl[2,i]],[0,IsoLumdkl[3,i]],color=RGBA(0.5,0.5,0.5,0.5)),1:size(hue_dkl_ilp,2))
plot!(p,IsoLumdkl[2,:],IsoLumdkl[3,:],aspectratio=:equal,color=IsoLumc,lw=1.5,markersize=6,marker=:circle,
markerstrokewidth=0,legend=false,xlabel="L-M",ylabel="S-(L+M)",title=title)
p
foreach(ext->savefig(joinpath(resultdir,"$title$ext")),figfmt)

## Exact DKL Hues colormap
cs = [RGBA(hue_dkl_ilp[:,i]...) for i in 1:size(hue_dkl_ilp,2)]
cm_dkl = (colors=[cs;cs[1]],notes="The exact DKL max_cone_contrast hues angled(0-360) in lum=$lum plane, constrained by a `ROGPG279Q` LCD Display.")
plotcolormap(cm_dkl.colors,xlabel="L-M",ylabel="S-(L+M)")

## Linear colormap generated from DKL L-M and S-(L+M) axis colors
L➕ = RGBA(maxc_dkl[:,2]...)
M➕ = RGBA(minc_dkl[:,2]...)
S➕ = RGBA(maxc_dkl[:,3]...)
S➖ = RGBA(minc_dkl[:,3]...)
cs = range(L➕,S➕,M➕,S➖,L➕,length=360)
cm_lidkl = (colors=cs,notes="The DKL hues linearly generated from max_cone_contrast hues of L-M and S-(L+M) axis at lum=$lum plane, constrained by a `ROGPG279Q` LCD Display.")
plotcolormap(cm_lidkl.colors,xlabel="L-M",ylabel="S-(L+M)")

## HSL equal angular distance hue[0:30:330] and equal energy white with matched luminance in CIE [x,y,Y] coordinates
hueangle_hsl = 0:30:330
hslhuewp = [0.63  0.54  0.42  0.34  0.3  0.27  0.22  0.17  0.15  0.2  0.32  0.5   0.33;
            0.34  0.41  0.5   0.57  0.6  0.5   0.33  0.15  0.07  0.1  0.16  0.27  0.33;
            1.0   1.0   1.0   1.0   1.0  1.0   1.0   1.0   1.0   1.0  1.0   1.0   1.0]
hslhuewp[3,:] .= 0.2
huewp_hsl = XYZToRGB*quavectors(xyY2XYZ(hslhuewp))
huewp_hsl = clamp.(huewp_hsl,0,1)
huewpc = [RGB(huewp_hsl[1:3,i]...) for i in 1:size(huewp_hsl,2)]
hue_hsl = huewp_hsl[:,1:end-1]
wp_hsl = repeat(huewp_hsl[:,end],inner=(1,size(hue_hsl,2)))

## Plot HSL Hues
IsoLumc = huewpc[1:end-1]
IsoLumhsl = [cos.(deg2rad.(hueangle_hsl)) sin.(deg2rad.(hueangle_hsl))]'

title="HSL_$(length(hueangle_hsl))Hue_Ym"
p=plot()
foreach(i->plot!(p,[0,IsoLumhsl[1,i]],[0,IsoLumhsl[2,i]],color=RGBA(0.5,0.5,0.5,0.5)),1:size(hue_hsl,2))
plot!(p,IsoLumhsl[1,:],IsoLumhsl[2,:],aspectratio=:equal,color=IsoLumc,lw=1.5,markersize=9,marker=:circle,
markerstrokewidth=0,legend=false,xlabel="S",ylabel="S",title=title)
p
foreach(ext->savefig(joinpath(resultdir,"$title$ext")),figfmt)

## HSL Hues colormap
l=0.4
cs = map(i->RGBA(HSLA(i,1,l,1)),0:360)
cm_hsl = (colors=cs, notes="The HSL max_saturated hues angled(0-360) at L=$l.")
plotcolormap(cm_hsl.colors,xlabel="S",ylabel="S")

## Save Color Maps
save(joinpath(resultdir,"colormaps.jld2"),"dkl_mcchue_l$lum",cm_dkl,"lidkl_mcchue_l$lum",cm_lidkl,"hsl_mshue_l$l",cm_hsl)

## Save color data
colordata = Dict{String,Any}("LMS_X"=>colorstring.([maxc_lms[:,1],minc_lms[:,1]]),
                            "LMS_Y"=>colorstring.([maxc_lms[:,2],minc_lms[:,2]]),
                            "LMS_Z"=>colorstring.([maxc_lms[:,3],minc_lms[:,3]]),
                            "LMS_XYZ_MichelsonContrast" => diag(mcc),
                            "LMS_Xmcc"=>colorstring.([mmaxc_lms[:,1],mminc_lms[:,1]]),
                            "LMS_Ymcc"=>colorstring.([mmaxc_lms[:,2],mminc_lms[:,2]]),
                            "LMS_Zmcc"=>colorstring.([mmaxc_lms[:,3],mminc_lms[:,3]]),
                            "LMS_XYZmcc_MichelsonContrast" => diag(mmcc),
                            "DKL_X"=>colorstring.([maxc_dkl[:,1],minc_dkl[:,1]]),
                            "DKL_Y"=>colorstring.([maxc_dkl[:,2],minc_dkl[:,2]]),
                            "DKL_Z"=>colorstring.([maxc_dkl[:,3],minc_dkl[:,3]]),
                            "HSL_HueAngle_Ym" => hueangle_hsl,
                            "HSL_Hue_Ym" => colorstring.([hue_hsl[:,i] for i in 1:size(hue_hsl,2)]),
                            "HSL_WP_Ym" => colorstring.([wp_hsl[:,i] for i in 1:size(wp_hsl,2)]),
                            "DKL_HueAngle_L0"=>hueangle_dkl_ilp,
                            "DKL_Hue_L0"=>colorstring.([hue_dkl_ilp[:,i] for i in 1:size(hue_dkl_ilp,2)]),
                            "DKL_WP_L0"=>colorstring.([wp_dkl_ilp[:,i] for i in 1:size(wp_dkl_ilp,2)]))
YAML.write_file(colordatapath,colordata)
