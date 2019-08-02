using Dierckx,LinearAlgebra
include("color_data.jl")

"Tristimulus values of digital color, based on spectral measurement and matching functions"
function matchcolors(C,λ,I,cmf)
    r = cmf[:,1];TC=Vector{Vector{Float64}}(undef,length(C))
    for i in 1:length(C)
        s = Spline1D(λ[i],I[i],k=3,bc="zero")(r)'
        TC[i] = [s*cmf[:,2],s*cmf[:,3],s*cmf[:,4]]
    end
    return TC,C
end
newcmf(cmf,m) = hcat(cmf[:,1],(m*cmf[:,2:end]')')
divsum(m)=m./sum(m,dims=1)
"Color matching Tristimulus values of single Wavelength unit spectral"
function matchlambda(λ,cmf)
    fs = [Spline1D(cmf[:,1],cmf[:,i+1],k=3,bc="zero") for i in 1:3]
    return map(f->f(λ),fs)
end
"Cone activations of digital colors, based on spectral measurement and cone fundamentals"
function lmsresponse(C,λ,I;observer=10)
    conef = observer==10 ? sscone10le : sscone2le
    matchcolors(C,λ,I,conef)
end
"CIE XYZ Tristimulus values of digital colors, based on spectral measurement and xyz matching functions"
function xyzresponse(C,λ,I;observer=10)
    if observer == 10
        conef = sscone10le
        m = LMSToXYZ10
    else
        conef = sscone2le
        m = LMSToXYZ2
    end
    matchcolors(C,λ,I,newcmf(conef,m))
end

TranslateXYZMatrix(;x=0,y=0,z=0) = TranslateXYZMatrix([x,y,z])
function TranslateXYZMatrix(t::Vector)
    tm =  [1.0 0.0 0.0   t[1];
           0.0 1.0 0.0   t[2];
           0.0 0.0 1.0   t[3];
           0.0 0.0 0.0 1.0]
end
function RotateXYZMatrix(rad::Real;dims=1)
    rads = [0.0,0.0,0.0]
    if 1<=dims<=3
        rads[dims]=rad
    end
    RotateXYZMatrix(rads)
end
function RotateXYZMatrix(rads::Vector)
    axis=nothing
    ri = findall(!iszero,rads)
    if length(ri) == 1 && ri[1] <= 3
        axis = ri[1]
    end
    isnothing(axis) && return Matrix{Float64}(I,4,4)
    c = cos(rads[axis])
    s = sin(rads[axis])
    if axis==1
      rm = [1.0 0.0 0.0 0.0;
            0.0   c  -s 0.0;
            0.0   s   c 0.0;
            0.0 0.0 0.0 1.0]
    elseif axis==2
      rm = [  c 0.0   s 0.0;
            0.0 1.0 0.0 0.0;
             -s 0.0   c 0.0;
            0.0 0.0 0.0 1.0]
    else
      rm = [  c  -s 0.0 0.0;
              s   c 0.0 0.0;
            0.0 0.0 1.0 0.0;
            0.0 0.0 0.0 1.0]
    end
end
quavectors(vs) = vcat(vs,ones(size(vs,2))')
trivectors(vs) = vs[1:3,:]
quamatrix(m) = vcat(hcat(m,zeros(3)),[0.0 0.0 0.0 1.0])
trimatrix(m) = m[1:3,1:3]
"Converting Matrix between digital RGB and LMS Space, based on spectral measurement and cone fundamentals"
function RGBLMSMatrix(C,λ,I;observer=10)
    TC,C = lmsresponse(C,λ,I,observer=observer)
    # Solve System of Linear Equations in form of Space Converting Matrix
    mc=hcat(C...);mtc=hcat(TC...)
    isqua = size(mc,1)==4
    if isqua
        mc = trivectors(mc)
    end
    RGBToLMS = mtc/mc
    LMSToRGB = inv(RGBToLMS)
    if isqua
        RGBToLMS = quamatrix(RGBToLMS)
        LMSToRGB = quamatrix(LMSToRGB)
    end
    return RGBToLMS,LMSToRGB
end
"Converting Matrix between digital RGB and CIE XYZ Space, based on spectral measurement and xyz matching functions"
function RGBXYZMatrix(C,λ,I;observer=10)
    TC,C = xyzresponse(C,λ,I,observer=observer)
    # Solve System of Linear Equations in form of Space Converting Matrix
    mc=hcat(C...);mtc=hcat(TC...)
    isqua = size(mc,1)==4
    if isqua
        mc = trivectors(mc)
    end
    RGBToXYZ = mtc/mc
    XYZToRGB = inv(RGBToXYZ)
    if isqua
        RGBToXYZ = quamatrix(RGBToXYZ)
        XYZToRGB = quamatrix(XYZToRGB)
    end
    return RGBToXYZ,XYZToRGB
end
"""
Converting Matrix between LMS and Cone Contrast Space
(DH Brainard, Cone contrast and opponent modulation color spaces, human color vision, 1996)
"""
function LMSContrastMatrix(bg)
    # translate origin to bg to get differential cone activation
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
function dLMSDKLMatrix(bg;cone=nothing,v=nothing,isnorm=true)
    wl = 1;wm = 1
    if !isnothing(cone) && !isnothing(v)
        wl,wm = v[:,2]'/cone[:,2:3]'
    end
    dLMSToDKL = [wl  wm           0;
                1 -bg[1]/bg[2] 0;
               -wl -wm (wl*bg[1] + wm*bg[2])/bg[3]]
    if isnorm
        # Each column of the inverse of dLMSToDKL is the differential LMS relative to bg that isolating each DKL mechanism
        dlms_dkliso = inv(dLMSToDKL)
        # Cone contrast relative to bg
        cc = dlms_dkliso./bg[1:3]
        # Pooled cone contrast of each differential LMS relative to bg that isolating each DKL mechanism
        pcc = [norm(cc[:,i]) for i in 1:3]
        # Scale differential LMS vector by its pooled cone contrast
        udlms_dkliso = dlms_dkliso./pcc'
        # Rescale dLMSToDKL so that differential LMS which isolating DKL mechanism and having unit pooled cone contrast will result unit DKL response
        dLMSToDKL = inv(dLMSToDKL*udlms_dkliso)*dLMSToDKL
    end
    DKLTodLMS = inv(dLMSToDKL)
    return dLMSToDKL,DKLTodLMS
end
"Converting Matrix between LMS and DKL[L+M, L-M, S-(L+M)] Space"
function LMSDKLMatrix(bg;observer=10,isnorm=true)
    if observer == 10
        conef = sscone10le
        v = v10le
    else
        conef = sscone2le
        v = v2le
    end
    dLMSToDKL,DKLTodLMS = dLMSDKLMatrix(bg,cone=conef,v=v,isnorm=isnorm)
    t = [1 0 0 -bg[1];
         0 1 0 -bg[2];
         0 0 1 -bg[3];
         0 0 0 1]
    LMSToDKL = quamatrix(dLMSToDKL)*t
    DKLToLMS = inv(LMSToDKL)
    return LMSToDKL,DKLToLMS
end
"""
Intersection point of a line and a plane.
points of a line are defined as a direction(Dₗ) through a point(Pₗ): P = Pₗ + λDₗ , where λ is a scaler
points of a plane are defined as a plane through a point(Pₚ) and with normal vector(Nₚ) : Nₚᵀ(P - Pₚ) = 0 , where Nᵀ is the transpose of N
"""
function intersectlineplane(Pₗ,Dₗ,Pₚ,Nₚ)
    NₚᵀDₗ = Nₚ'*Dₗ
    NₚᵀDₗ == 0 && return []
    λ = Nₚ'*(Pₚ - Pₗ) / NₚᵀDₗ
    return Pₗ + λ*Dₗ
end
"""
Intersection points of a line and the six faces of the unit cube with origin as a vertex and three axies as edges.
points of a line are defined as a direction(Dₗ) through a point(Pₗ)
"""
function intersectlineunitorigincube(Pₗ,Dₗ)
    ips=[]
    ps = [zeros(3,3) ones(3,3)]
    ns = [Matrix{Float64}(I,3,3) Matrix{Float64}(I,3,3)]
    for i in 1:6
        p = intersectlineplane(Pₗ,Dₗ,ps[:,i],ns[:,i])
        if !isempty(p) && all(i->-eps()<=i<=1+eps(),p)
            push!(ips,p)
        end
    end
    return hcat(ips...)
end
"""
Points of a line segment defined by two points P₀ and P₁
d: points density of unit line length
"""
function linepoints(P₀,P₁;d=10)
    d = max(1,d)
    Dₗ = P₁-P₀
    l =norm(Dₗ)
    hcat([P₀.+λ*Dₗ for λ in 0:1/(d*l):1]...)
end
function XYZ2xyY(m)
    xyz = divsum(trivectors(m))
    xyz[3,:]=m[2,:]
    return xyz
end
function desaturate2gamut!(vs)
    for i in 1:size(vs,2)
        minc = minimum(vs[:,i])
        if minc<0
            vs[:,i].-=minc
        end
        maxc = maximum(vs[:,i])
        if maxc>0
            vs[:,i]./=maxc
        end
    end
    return vs
end
function cam16nonlinearcompression(x,Fl)
    p = (Fl*abs(x)/100)^0.42
    sign(x)*400p/(27.13 + p) + 0.1
end
"""
CAM16 Viewing Conditions

W: Adopted white in test illuminant [Xw, Yw, Zw]
Yb: Background in test conditions
La: Luminance of test adapting field (cd/m2)
Surround: Surround condition {Average, Dim, Dark}, Nc and F are functions of c, and their values can be linearly interpolated
"""
function cam16view(;W=[95.05,100,108.88],Surround=:Average,La=40,Yb=20)
    F,c,Nc = cam16surround[cam16surround[:,1].==Surround,2:end]
    k = 1/(5La + 1)
    Fl = (0.2*k^4)*5La + (0.1*(1 - k^4)^2)*(5La)^(1/3)

    Yw = W[2]
    n = Yb/Yw
    Nbb = Ncb = 0.725*(1/n)^0.2
    z = 1.48 + sqrt(n)
    D = clamp(F * (1 - (1/3.6)*exp(-(La+42)/92)), 0, 1)

    RGBw = cat16*W
    Da = Diagonal([D*Yw/RGBw[i] + 1 - D for i in 1:3])
    RGBcw = Da*RGBw
    RGBaw = cam16nonlinearcompression.(RGBcw,Fl)
    Aw = Nbb*(([2 1 1/20]*RGBaw)[1] - 0.305)

    return (c=c,Nc=Nc,Nbb=Nbb,Ncb=Ncb,Fl=Fl,n=n,z=z,Da=Da,Aw=Aw)
end
"""
Convert CIE XYZ to CIE Color Appearance Model 2016
Li, C., Li, Z., Wang, Z., Xu, Y., Luo, M.R., Cui, G., Melgosa, M., Brill, M.H., and Pointer, M. (2017). Comprehensive color solutions: CAM16, CAT16, and CAM16‐UCS.

return:
J is the lightness
C is the chroma
h is the hue angle
Q is the brightness
M is the colourfulness
s is the saturation
"""
function XYZ2CAM16(XYZ;Fl,Nc,Ncb,Nbb,n,c,z,Da,Aw)
    # Chromatic Adaptation
    RGBc = Da*cat16*XYZ
    # Non Linear Response Compression
    RGBa = cam16nonlinearcompression.(RGBc,Fl)
    # Perceptual Attribute Correlates
    a = [1 -12/11 1/11]*RGBa
    b = [1/9 1/9 -2/9]*RGBa
    h = mod.(2pi .+ atan.(b,a), 2pi)

    h′ = rad2deg.(h)
    i = h′ .< cam16uniquehue[1,1]
    h′[i] = h′[i] .+ 360

    #et = 0.25 .* (cos.(deg2rad.(h′) .+ 2) .+ 3.8)
    et = 0.25 .* (cos.(h .+ 2) .+ 3.8)

    t = (50000/13)*Nc*Ncb .* et .* sqrt.(a.^2 .+ b.^2) ./ ([1 1 21/20]*RGBa)
    A = Nbb.*([2 1 1/20]*RGBa .- 0.305)

    J = 100 .* (A./Aw).^(c*z)
    Q = (4/c) .* sqrt.(J./100) .* (Aw + 4) .* Fl^0.25
    C = t.^0.9 .* sqrt.(J./100) .* (1.64 - 0.29^n)^0.73
    M = (Fl^0.25).*C
    s = 100 .* sqrt.(M./Q)

    return (J=J,Q=Q,C=C,M=M,h=h,s=s)
end
"""
CAM16 Uniform Color Space in polar[J′, M′, h] or cartesian[J′, a′, b′]
Luo, M.R., Cui, G., and Li, C. (2006). Uniform colour spaces based on CIECAM02 colour appearance model. Color Research & Application 31, 320–330.
"""
function CAM16UCS(J,M,h;c1=0.007,c2=0.0228,form=:cartesian)
    J′ = J.*(1 + 100c1) ./ (1 .+ c1.*J)
    M′ = (1/c2).*log.(1 .+ c2.*M)
    a′ = M′.*cos.(h)
    b′ = M′.*sin.(h)
    return form == :cartesian ? vcat(J′,a′,b′) : vcat(J′,M′,h)
end
function CAM16UCSinv(ucs;c1=0.007,c2=0.0228,form=:cartesian)
    J′ = ucs[1,:]
    if form == :cartesian
        M′ = sqrt.(ucs[2,:].^2 .+ ucs[3,:].^2)
        h = mod.(2pi .+ atan.(ucs[3,:],ucs[2,:]), 2pi)
    else
        M′ = ucs[2,:]
        h = ucs[3,:]
    end
    M = (exp.(c2.*M′) .- 1) ./ c2
    J = J′ ./ (1 .+ 100c1 .- c1.*J′)
    return (J=J,M=M,h=h)
end
function cam16nonlinearcompressioninv(x,Fl)
    p = x-0.1
    sign(p)*(100/Fl)*(27.13*abs(p) / (400-abs(p)))^(1/0.42)
end
"""
Convert CIE Color Appearance Model 2016 to CIE XYZ
"""
function CAM162XYZ(J,M,h;Fl,Nc,Ncb,Nbb,n,c,z,Da,Aw)
    C = M ./ (Fl^0.25)

    t = (C ./ (sqrt.(J./100) .* (1.64 - 0.29^n)^0.73)).^(1/0.9)
    et = 0.25 .* (cos.(h .+ 2) .+ 3.8)
    A = Aw.*(J./100).^(1/(c*z))
    p2 = A./Nbb .+ 0.305
    p3 = 21/20
    sh = sin.(h)
    ch = cos.(h)

    a=b=zeros(length(h))
    for i in 1:length(h)
        if t[i] !=0
            p1 = 50000/13 * Nc*Ncb * et[i] * (1 / t[i])
            if abs(sh[i]) >= abs(ch[i])
                p4 = p1/sh[i]
                b[i] = p2[i]*(2+p3)*(460/1403) / ( p4+(2+p3)*(220/1403)*(ch[i]/sh[i]) - (27/1403) + p3*(6300/1403) )
                a[i] = b[i]*ch[i]/sh[i]
            else
                p5 = p1/ch[i]
                a[i] = p2[i]*(2+p3)*(460/1403) / ( p5+(2+p3)*(220/1403) - (27/1403 - p3*6300/1403)*sh[i]/ch[i] )
                b[i] = a[i]*sh[i]/ch[i]
            end
        end
    end

    RGBa = [460/1403 451/1403 288/1403;
            460/1403 -891/1403 -261/1403;
            460/1403 -220/1403 -6300/1403]*([p2 a b]')
    RGBc = cam16nonlinearcompressioninv.(RGBa,Fl)
    #XYZ = cat16inv*inv(Da)*RGBc
    XYZ = cat16\Da\RGBc
end
