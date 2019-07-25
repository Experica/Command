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
function rgblmsresponse(C,λ,I;observer=10)
    conef = observer==10 ? sscone10le : sscone2le
    matchcolors(C,λ,I,conef)
end
"CIE XYZ Tristimulus values of digital colors, based on spectral measurement and xyz matching functions"
function rgbxyzresponse(C,λ,I;observer=10)
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
quavectors(trivecs) = vcat(trivecs,ones(size(trivecs,2))')
trivectors(quavecs) = quavecs[1:3,:]
quamatrix(trimat) = vcat(hcat(trimat,zeros(3)),[0.0 0.0 0.0 1.0])
trimatrix(quamat) = quamat[1:3,1:3]
"Converting Matrix between RGB and LMS Space, based on spectral measurement and cone fundamentals"
function RGBLMSMatrix(C,λ,I;observer=10)
    TC,C = rgblmsresponse(C,λ,I,observer=observer)
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
"Converting Matrix between RGB and CIE XYZ Space, based on spectral measurement and xyz matching functions"
function RGBXYZMatrix(C,λ,I;observer=10)
    TC,C = rgbxyzresponse(C,λ,I,observer=observer)
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
        wl,wm = v[:,2]/cone[:,2:3]
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
points of a plane are defined as a plane with normal vector(Nₚ) through a point(Pₚ): Nₚᵀ(P - Pₚ) = 0 , where Nᵀ is the transpose of N
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
    if length(ips)==1
        println("empty with point: $Pₗ and direction $Dₗ")
        for i in 1:6
            p = intersectlineplane(Pₗ,Dₗ,ps[:,i],ns[:,i])
            if !isempty(p)
                println("P is $p")
            end
        end
    end
    return hcat(ips...)
end





function XYZ2xyY(m)
    xyz = divsum(trivectors(m))
    xyz[3,:]=m[2,:]
    return xyz
end
