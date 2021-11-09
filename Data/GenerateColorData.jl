using ColorLab,LinearAlgebra,YAML,Plots,NeuroAnalysis

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
bg = RGBToLMS*[th;1]
LMSToContrast,ContrastToLMS = LMSContrastMatrix(bg)
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
hueangle_dkl_ilp = [0,30,60,75,80,83,90,95,101,105,110,120,150]
append!(hueangle_dkl_ilp,hueangle_dkl_ilp .+ 180)
# rotate l+ direction around l+m axis within the `lum` Isoluminance plane
DKLIsoLumRGBVec = trivectors(hcat(map(i->DKLToRGB*RotateXYZMatrix(deg2rad(i),dims=1)*[0,1,0,0], hueangle_dkl_ilp)...))
# find Intersections of Isoluminance directions with faces of unit RGB cube
anglec = hcat(map(i->intersectlineunitorigincube((DKLToRGB*[lum,0,0,1])[1:3],DKLIsoLumRGBVec[:,i]),1:size(DKLIsoLumRGBVec,2))...)
hue_dkl_ilp = clamp.(quavectors(anglec),0,1)
wp_dkl_ilp = repeat([th;1],inner=(1,size(hue_dkl_ilp,2)))

# Plot DKL Hues
IsoLumc = [RGB(hue_dkl_ilp[1:3,i]...) for i in 1:size(hue_dkl_ilp,2)]
IsoLumdkl = RGBToDKL*hue_dkl_ilp

title="DKL_$(length(hueangle_dkl_ilp))Hue_L$lum"
p=plot()
foreach(i->plot!(p,[0,IsoLumdkl[2,i]],[0,IsoLumdkl[3,i]],color=RGBA(0.5,0.5,0.5,0.5)),1:size(hue_dkl_ilp,2))
plot!(p,IsoLumdkl[2,:],IsoLumdkl[3,:],aspectratio=:equal,color=IsoLumc,lw=1.5,markersize=6,marker=:circle,
markerstrokewidth=0,legend=false,xlabel="L-M",ylabel="S-(L+M)",title=title)
p
foreach(ext->savefig(joinpath(resultdir,"$title$ext")),figfmt)

## HSL equal angular distance hue[0:30:330] and equal energy white with matched luminance in CIE [x,y,Y] coordinates
# Name:     R:1           Y:3           G:5                         B:9                         WP
hslhues =  [0.63   0.54   0.42   0.34   0.3    0.27   0.22   0.17   0.15   0.2    0.32   0.5    0.33;
            0.34   0.41   0.5    0.57   0.6    0.5    0.33   0.15   0.07   0.1    0.16   0.27   0.33;
            1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0]

# Computed RGBAs
hslhues[3,:] .= 0.2
hues_hsl = XYZToRGB*quavectors(xyY2XYZ(hslhues))
hues_hsl = clamp.(hues_hsl,0,1)

# Mannual adjusted RGBAs
hues_hsl = [0.360  0.216  0.086  0.027  0.006  0.000  0.001  0.004  0.000  0.120  0.259  0.340  0.096;
            0.009  0.049  0.088  0.105  0.112  0.110  0.101  0.077  0.025  0.027  0.014  0.009  0.0762;
            0.002  0.004  0.001  0.002  0.001  0.030  0.110  0.360  1.000  0.550  0.265  0.067  0.081;
            1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0    1.0]

hueangle_hsl = 0:30:330
hue_hsl = hues_hsl[:,1:end-1]
wp_hsl = repeat(hues_hsl[:,end],inner=(1,size(hue_hsl,2)))

# Plot HSL Hues
IsoLumc = [RGB(hue_hsl[1:3,i]...) for i in 1:size(hue_hsl,2)]
IsoLumhsl = [cos.(deg2rad.(hueangle_hsl)) sin.(deg2rad.(hueangle_hsl))]'

title="HSL_$(length(hueangle_hsl))Hue_Ym"
p=plot()
foreach(i->plot!(p,[0,IsoLumhsl[1,i]],[0,IsoLumhsl[2,i]],color=RGBA(0.5,0.5,0.5,0.5)),1:size(hue_hsl,2))
plot!(p,IsoLumhsl[1,:],IsoLumhsl[2,:],aspectratio=:equal,color=IsoLumc,lw=1.5,markersize=9,marker=:circle,
markerstrokewidth=0,legend=false,xlabel="S",ylabel="S",title=title)
p
foreach(ext->savefig(joinpath(resultdir,"$title$ext")),figfmt)

## Save color data
colordata = Dict{String,Any}("LMS_X"=>colorstring.([minc_lms[:,1],maxc_lms[:,1]]),
                            "LMS_Y"=>colorstring.([minc_lms[:,2],maxc_lms[:,2]]),
                            "LMS_Z"=>colorstring.([minc_lms[:,3],maxc_lms[:,3]]),
                            "LMS_X_WP"=>colorstring.([[th;1],[th;1]]),
                            "LMS_Y_WP"=>colorstring.([[th;1],[th;1]]),
                            "LMS_Z_WP"=>colorstring.([[th;1],[th;1]]),
                            "LMS_XYZ_MichelsonContrast" => diag(mcc),
                            "LMS_Xmcc"=>colorstring.([mminc_lms[:,1],mmaxc_lms[:,1]]),
                            "LMS_Ymcc"=>colorstring.([mminc_lms[:,2],mmaxc_lms[:,2]]),
                            "LMS_Zmcc"=>colorstring.([mminc_lms[:,3],mmaxc_lms[:,3]]),
                            "LMS_Xmcc_WP"=>colorstring.([[th;1],[th;1]]),
                            "LMS_Ymcc_WP"=>colorstring.([[th;1],[th;1]]),
                            "LMS_Zmcc_WP"=>colorstring.([[th;1],[th;1]]),
                            "LMS_XYZmcc_MichelsonContrast" => diag(mmcc),
                            "DKL_X"=>colorstring.([minc_dkl[:,1],maxc_dkl[:,1]]),
                            "DKL_Y"=>colorstring.([minc_dkl[:,2],maxc_dkl[:,2]]),
                            "DKL_Z"=>colorstring.([minc_dkl[:,3],maxc_dkl[:,3]]),
                            "DKL_X_WP"=>colorstring.([[th;1],[th;1]]),
                            "DKL_Y_WP"=>colorstring.([[th;1],[th;1]]),
                            "DKL_Z_WP"=>colorstring.([[th;1],[th;1]]),
                            "HSL_HueYm_Angle" => hueangle_hsl,
                            "HSL_HueYm" => colorstring.([hue_hsl[:,i] for i in 1:size(hue_hsl,2)]),
                            "HSL_HueYm_WP" => colorstring.([wp_hsl[:,i] for i in 1:size(wp_hsl,2)]),
                            "HSL_RGYm" => colorstring.([hue_hsl[:,i] for i in (1,5)]),
                            "HSL_RGYm_WP" => colorstring.([wp_hsl[:,i] for i in (1,5)]),
                            "HSL_YBYm" => colorstring.([hue_hsl[:,i] for i in (3,9)]),
                            "HSL_YBYm_WP" => colorstring.([wp_hsl[:,i] for i in (3,9)]),
                            "HSL_RBYm" => colorstring.([hue_hsl[:,i] for i in (1,9)]),
                            "HSL_RBYm_WP" => colorstring.([wp_hsl[:,i] for i in (1,9)]),
                            "HSL_YGYm" => colorstring.([hue_hsl[:,i] for i in (3,5)]),
                            "HSL_YGYm_WP" => colorstring.([wp_hsl[:,i] for i in (3,5)]),
                            "DKL_HueL0_Angle"=>hueangle_dkl_ilp,
                            "DKL_HueL0"=>colorstring.([hue_dkl_ilp[:,i] for i in 1:size(hue_dkl_ilp,2)]),
                            "DKL_HueL0_WP"=>colorstring.([wp_dkl_ilp[:,i] for i in 1:size(wp_dkl_ilp,2)]),
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







## HSL hues in DKL
hues_hsl=[0.3137 0 0 1;
           0.1922 0.03137 0 1;
           0.07059 0.07059 0 1;
           0.01569 0.08627 0 1;
           0 0.0902 0 1;
           0 0.08627 0.01961 1;
           0 0.07843 0.09412 1;
           0 0.0549 0.3608 1;
           0 0 0.9843 1;
           0.1255 0 0.5686 1;
           0.2314 0 0.2353 1;
           0.2902 0 0.05882 1;
           0.07843 0.05882 0.06275 1]'
huewp_hsl_dkl = trivectors(RGBToDKL*hues_hsl)

plot(huewp_hsl_dkl[2,1:end-1],huewp_hsl_dkl[3,1:end-1],markersize=9,marker=:circle,color=cm_hsl.colors[1:30:331],xlabel="L-M",ylabel="S-(L+M)",frame=:origin,
annotations=[(huewp_hsl_dkl[2,i]+0.05,huewp_hsl_dkl[3,i],text(hueangle_hsl[i],7,:gray5,:bottom,:left)) for i in eachindex(hueangle_hsl)])
scatter!(huewp_hsl_dkl[2,end:end],huewp_hsl_dkl[3,end:end],markersize=9,marker=:circle,color=:gray,leg=false)
foreach(ext->savefig(joinpath(resultdir,"HSL_$(length(hueangle_hsl))Hue_Ym_KL$ext")),figfmt)

plot(huewp_hsl_dkl[2,1:end-1],huewp_hsl_dkl[1,1:end-1],markersize=9,marker=:circle,color=cm_hsl.colors[1:30:331],xlabel="L-M",ylabel="Lum",frame=:origin,
annotations=[(huewp_hsl_dkl[2,i]+0.05,huewp_hsl_dkl[1,i],text(hueangle_hsl[i],7,:gray5,:bottom,:left)) for i in eachindex(hueangle_hsl)])
scatter!(huewp_hsl_dkl[2,end:end],huewp_hsl_dkl[1,end:end],markersize=9,marker=:circle,color=:gray,leg=false)
foreach(ext->savefig(joinpath(resultdir,"HSL_$(length(hueangle_hsl))Hue_Ym_KD$ext")),figfmt)



## Cone Isolating colormap
L➕ = RGBA(maxc_lms[:,1]...)
L➖ = RGBA(minc_lms[:,1]...)
M➕ = RGBA(maxc_lms[:,2]...)
M➖ = RGBA(minc_lms[:,2]...)
S➕ = RGBA(maxc_lms[:,3]...)
S➖ = RGBA(minc_lms[:,3]...)

cs = range(L➖,L➕,length=360)
cm_liso = (colors=cs,notes="The L Cone Isolating colors linearly generated between[min,max], constrained by a `$displayname` LCD Display.")

cs = range(M➖,M➕,length=360)
cm_miso = (colors=cs,notes="The M Cone Isolating colors linearly generated between[min,max], constrained by a `$displayname` LCD Display.")

cs = range(S➖,S➕,length=360)
cm_siso = (colors=cs,notes="The S Cone Isolating colors linearly generated between[min,max], constrained by a `$displayname` LCD Display.")

cs = range(L➖,RGBA(1,1,1,1.0),L➕,length=360)
cm_lisow = (colors=cs,notes="The L Cone Isolating colors linearly generated between[min,white,max], constrained by a `$displayname` LCD Display.")

cs = range(M➖,RGBA(1,1,1,1.0),M➕,length=360)
cm_misow = (colors=cs,notes="The M Cone Isolating colors linearly generated between[min,white,max], constrained by a `$displayname` LCD Display.")

cs = range(S➖,RGBA(1,1,1,1.0),S➕,length=360)
cm_sisow = (colors=cs,notes="The S Cone Isolating colors linearly generated between[min,white,max], constrained by a `$displayname` LCD Display.")

cs = range(L➖,RGBA(0,0,0,1.0),L➕,length=360)
cm_lisob = (colors=cs,notes="The L Cone Isolating colors linearly generated between[min,black,max], constrained by a `$displayname` LCD Display.")

cs = range(M➖,RGBA(0,0,0,1.0),M➕,length=360)
cm_misob = (colors=cs,notes="The M Cone Isolating colors linearly generated between[min,black,max], constrained by a `$displayname` LCD Display.")

cs = range(S➖,RGBA(0,0,0,1.0),S➕,length=360)
cm_sisob = (colors=cs,notes="The S Cone Isolating colors linearly generated between[min,black,max], constrained by a `$displayname` LCD Display.")

## DKL Axis Isolating colormap
Lum➕ = RGBA(maxc_dkl[:,1]...)
Lum➖ = RGBA(minc_dkl[:,1]...)
L_M➕ = RGBA(maxc_dkl[:,2]...)
L_M➖ = RGBA(minc_dkl[:,2]...)
S_LM➕ = RGBA(maxc_dkl[:,3]...)
S_LM➖ = RGBA(minc_dkl[:,3]...)

cs = range(Lum➖,Lum➕,length=360)
cm_lumiso = (colors=cs,notes="The DKL Lum Axis colors linearly generated between[min,max], constrained by a `$displayname` LCD Display.")

cs = range(L_M➖,L_M➕,length=360)
cm_lmiso = (colors=cs,notes="The DKL L-M Axis colors linearly generated between[min,max], constrained by a `$displayname` LCD Display.")

cs = range(S_LM➖,S_LM➕,length=360)
cm_slmiso = (colors=cs,notes="The DKL S-(L+M) Axis colors linearly generated between[min,max], constrained by a `$displayname` LCD Display.")

cs = range(L_M➖,RGBA(1,1,1,1.0),L_M➕,length=360)
cm_lmisow = (colors=cs,notes="The DKL L-M Axis colors linearly generated between[min,white,max], constrained by a `$displayname` LCD Display.")

cs = range(S_LM➖,RGBA(1,1,1,1.0),S_LM➕,length=360)
cm_slmisow = (colors=cs,notes="The DKL S-(L+M) Axis colors linearly generated between[min,white,max], constrained by a `$displayname` LCD Display.")

cs = range(L_M➖,RGBA(0,0,0,1.0),L_M➕,length=360)
cm_lmisob = (colors=cs,notes="The DKL L-M Axis colors linearly generated between[min,black,max], constrained by a `$displayname` LCD Display.")

cs = range(S_LM➖,RGBA(0,0,0,1.0),S_LM➕,length=360)
cm_slmisob = (colors=cs,notes="The DKL S-(L+M) Axis colors linearly generated between[min,black,max], constrained by a `$displayname` LCD Display.")

## DKL IsoLum Plane colormap
angles = 0:360;lum=0
DKLIsoLumRGBVec = trivectors(hcat(map(i->DKLToRGB*RotateXYZMatrix(deg2rad(i),dims=1)*[0,1,0,0], angles)...))
anglec = hcat(map(i->intersectlineunitorigincube((DKLToRGB*[lum,0,0,1])[1:3],DKLIsoLumRGBVec[:,i]),1:size(DKLIsoLumRGBVec,2))...)
dkl_ilp = clamp.(quavectors(anglec),0,1)

cs = [RGBA(dkl_ilp[:,i]...) for i in 1:size(dkl_ilp,2)]
cm_lumiso_plane = (colors=cs,notes="The exact DKL max_cone_contrast colors angled[0,360] in lum=$lum plane, constrained by a `$displayname` LCD Display.")
plotcolormap(cm_lumiso_plane.colors,xlabel="L-M",ylabel="S-(L+M)")

# Linear colormap generated from DKL L-M and S-(L+M) axis colors
cs = range(L_M➕,S_LM➕,L_M➖,S_LM➖,L_M➕,length=365)
cm_lumiso_planel = (colors=cs,notes="The DKL colors linearly generated from max_cone_contrast colors of L-M and S-(L+M) axis at lum=$lum plane, constrained by a `$displayname` LCD Display.")
plotcolormap(cm_lumiso_planel.colors,xlabel="L-M",ylabel="S-(L+M)")

## DKL IsoLM Plane colormap
angles = 0:360;lm=0
DKLIsoLMRGBVec = trivectors(hcat(map(i->DKLToRGB*RotateXYZMatrix(deg2rad(i),dims=2)*[0,0,1,0], angles)...))
anglec = hcat(map(i->intersectlineunitorigincube((DKLToRGB*[0,lm,0,1])[1:3],DKLIsoLMRGBVec[:,i]),1:size(DKLIsoLMRGBVec,2))...)
dkl_ilmp = clamp.(quavectors(anglec),0,1)

cs = [RGBA(dkl_ilmp[:,i]...) for i in 1:size(dkl_ilmp,2)]
cm_lmiso_plane = (colors=cs,notes="The exact DKL max_cone_contrast colors angled[0,360] in lm=$lm plane, constrained by a `$displayname` LCD Display.")
plotcolormap(cm_lmiso_plane.colors,xlabel="S-(L+M)",ylabel="Lum")

# Linear colormap generated from DKL S-(L+M) and Lum axis colors
cs = range(S_LM➕,Lum➕,S_LM➖,Lum➖,S_LM➕,length=365)
cm_lmiso_planel = (colors=cs,notes="The DKL colors linearly generated from max_cone_contrast colors of S-(L+M) and Lum axis at lm=$lm plane, constrained by a `$displayname` LCD Display.")
plotcolormap(cm_lmiso_planel.colors,xlabel="S-(L+M)",ylabel="Lum")

## DKL IsoSLM Plane colormap
angles = 0:360;slm=0
DKLIsoSLMRGBVec = trivectors(hcat(map(i->DKLToRGB*RotateXYZMatrix(deg2rad(i),dims=3)*[0,-1,0,0], angles)...))
anglec = hcat(map(i->intersectlineunitorigincube((DKLToRGB*[0,0,slm,1])[1:3],DKLIsoSLMRGBVec[:,i]),1:size(DKLIsoSLMRGBVec,2))...)
dkl_islmp = clamp.(quavectors(anglec),0,1)

cs = [RGBA(dkl_islmp[:,i]...) for i in 1:size(dkl_islmp,2)]
cm_slmiso_plane = (colors=cs,notes="The exact DKL max_cone_contrast colors angled[0,360] in slm=$slm plane, constrained by a `$displayname` LCD Display.")
plotcolormap(cm_slmiso_plane.colors,xlabel="M-L",ylabel="Lum")

# Linear colormap generated from DKL M-L and Lum axis colors
cs = range(L_M➖,Lum➕,L_M➕,Lum➖,L_M➖,length=365)
cm_slmiso_planel = (colors=cs,notes="The DKL colors linearly generated from max_cone_contrast colors of M-L and Lum axis at slm=$slm plane, constrained by a `$displayname` LCD Display.")
plotcolormap(cm_slmiso_planel.colors,xlabel="M-L",ylabel="Lum")

## HSL Hues colormap
l=0.4
cs = map(i->RGBA(HSLA(i,1,l,1)),0:360)
cm_hsl_hue = (colors=cs, notes="The HSL max_saturated hues angled[0,360] at L=$l.")
plotcolormap(cm_hsl_hue.colors,xlabel="S",ylabel="S")

## Save Color Maps
save(joinpath(resultdir,"colormaps.jld2"),"lms_mccliso",cm_liso,"lms_mccmiso",cm_miso,"lms_mccsiso",cm_siso,
    "lms_mcclisow",cm_lisow,"lms_mccmisow",cm_misow,"lms_mccsisow",cm_sisow,
    "lms_mcclisob",cm_lisob,"lms_mccmisob",cm_misob,"lms_mccsisob",cm_sisob,
    "dkl_mcclumiso",cm_lumiso,"dkl_mcclmiso",cm_lmiso,"dkl_mccslmiso",cm_slmiso,
    "dkl_mcclmisow",cm_lmisow,"dkl_mccslmisow",cm_slmisow,
    "dkl_mcclmisob",cm_lmisob,"dkl_mccslmisob",cm_slmisob,
    "dkl_mcchue_l$lum",cm_lumiso_plane,"lidkl_mcchue_l$lum",cm_lumiso_planel,
    "dkl_mcchue_lm$lm",cm_lmiso_plane,"lidkl_mcchue_lm$lm",cm_lmiso_planel,
    "dkl_mcchue_slm$slm",cm_slmiso_plane,"lidkl_mcchue_slm$slm",cm_slmiso_planel,"hsl_mshue_l$l",cm_hsl_hue)
