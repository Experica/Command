using FileIO,JLD2,MAT,NeuroAnalysis,Images,Interpolations,Random,StatsBase,StatsPlots,ProgressMeter,MsgPack,YAML

function imagepatch(img,ppd,sizedeg;topleftdeg=nothing)
    sizepx = round.(Int,sizedeg.*ppd)
    isize = size(img)
    isizepx = isize[1:2]
    any(sizepx.>isizepx) && error("Patch$sizepx is larger than image$isizepx.")

    maxtopleftpx = isizepx .- sizepx .+ 1
    if isnothing(topleftdeg)
        topleftpx = rand.(map(i->1:i,maxtopleftpx))
    end
    ppx = map((i,j)->(0:j-1).+i,topleftpx,sizepx)
    if length(isize)==2
        patch = img[ppx...]
    else
        patch = img[ppx...,map(i->1:i,isize[3:end])...]
    end
    return patch
end

function newimageset(imgdbroot,imgfile,ppd;n=100,sizedeg=(3,3),sizepx=(32,32),s=ones(1,1,3),isnorm=true)
    imgfiles=[]
    for (root,dirs,files) in walkdir(imgdbroot)
        append!(imgfiles,joinpath.(root,filter(f->occursin(imgfile,f),files)))
    end
    isempty(imgfiles) && error("No valid image file found in the image database.")

    imgpatchs=[]
    @showprogress desc="Sampling Images ... " for i in 1:n
        file = rand(imgfiles)
        ext = splitext(file)[2]
        if ext == ".mat"
            img = first(matread(file)).second
        end
        # ip = s.*imresize(imagepatch(img,ppd,sizedeg),sizepx,method=Lanczos()) # same contrast, but noisy
        ip = s.*imresize_antialiasing(imagepatch(img,ppd,sizedeg),sizepx) # smooth, but reduce contrast
        any(isnan.(ip)) || push!(imgpatchs,ip)
    end
    if isnorm
        lim = mapreduce(i->maximum(i),max,imgpatchs)
        imgpatchs = map(i->i/lim,imgpatchs)
    end
    return imgpatchs
end

function sampleimageset(imgsets...;ps=fill(1.0/length(imgsets),length(imgsets)),n=100)
    ns = round.(Int,ps.*n)
    imgs = []
    @showprogress desc="Picking Images ... " for j in eachindex(ns)
        is = sample(1:length(imgsets[j]),ns[j],replace=false)
        append!(imgs,imgsets[j][is])
    end
    shuffle!(imgs)
    return imgs
end

lumlms(lms;w=[0.68990272;;;0.34832189;;;0])=dropdims(sum(lms.*w,dims=3),dims=3)

function excludesimilarimage!(imgset;alpha=0.8,lumfun=lumlms,simfun=MSSSIM())
    di = []
    for i in eachindex(imgset)
        (any(isnan.(imgset[i])) || any(isinf.(imgset[i]))) && push!(di,i)
    end
    deleteat!(imgset,di)

    l=length(imgset);di=falses(l);lumimgset=lumfun.(imgset)
    lumlim = mapreduce(i->maximum(i),max,lumimgset)
    lumimgset = map(i->i/lumlim,lumimgset)
    @showprogress desc="Checking Images ... " for i in 1:(l-1)
        di[i] && continue
        Threads.@threads for j in (i+1):l
            di[j] && continue
            simfun(lumimgset[j],lumimgset[i]) > alpha && (di[j]=true)
        end
    end
    deleteat!(imgset,di);deleteat!(lumimgset,di)
    imgset,lumimgset
end



function saveunityrawtexture(fn,imgset)
    ds = size(imgset[1]);imgs=[]
    if length(ds) < 3
        pbit = 8
        for i in imgset
            # Unity Texture2D RawData unfold from left->right, then bottom->up
            ps = vec(reverse(permutedims(i),dims=2))
            # Pack Gray in UInt8
            ps = [reinterpret(N0f8(p)) for p in ps]
            push!(imgs,ps)
        end
    else
        pbit = 32
        for i in imgset
            # Unity Texture2D RawData unfold from left->right, then bottom->up
            ps = vec(reverse(permutedims(i),dims=2))
            # Pack RGBA32 in UInt32
            ps = [parse(UInt32,"0x"*hex(RGBA{N0f8}(p,p,p,1),:rrggbbaa)) for p in ps]
            push!(imgs,ps)
        end
    end
    imgdata = Dict("ImageSize"=>[map(i->UInt16(i),ds)...],"Images"=>imgs)
    # MessagePack Binary File
    write("$fn.mpis$pbit", pack(imgdata))
    return imgdata
end

t=first(values(matread(raw"S:\McGillCCIDB\Animals\merry_mexico0125_LMS.mat")))


ti=colorview(RGB,(clampscale(i,3) for i in eachslice(t,dims=3))...)

ti=colorview(RGB,rand(3,128,128))



stimuliroot = "S:/"
## Imageset from McGillCCIDB
idb = "McGillCCIDB"
itype = "LMS"
ppd = 90 # should be close to UPennNIDB
n = 60000
sizedeg = (3,3)
sizepx = (64,64)
imgset_MG = newimageset(joinpath(stimuliroot,idb),Regex("\\w*$itype.mat"),ppd;n,sizedeg,sizepx)
imgsetpath = joinpath(stimuliroot,"$(idb)_$(itype)_n$(n)_sizedeg$(sizedeg)_sizepx$(sizepx).jld2")
save(imgsetpath,"imgset",imgset_MG)
imgset_MG = load(imgsetpath,"imgset")

## Imageset from UPennNIDB
idb = "UPennNIDB"
itype = "LMS"
ppd = 92
n = 60000
sizedeg = (3,3)
sizepx = (64,64)
s = [1/1.75e5;;;1/1.6e5;;;1/3.49e4] # isomerization rate used in "UPennNIDB" L, M, S = 1.75e5, 1.6e5, 3.49e4
imgset_UP = newimageset(joinpath(stimuliroot,idb),Regex("\\w*$itype.mat"),ppd;n,sizedeg,sizepx,s)
imgsetpath = joinpath(stimuliroot,"$(idb)_$(itype)_n$(n)_sizedeg$(sizedeg)_sizepx$(sizepx).jld2")
save(imgsetpath,"imgset",imgset_UP)
imgset_UP = load(imgsetpath,"imgset")


colorview(RGB,(clampscale(i,3) for i in eachslice(imgset_UP[3],dims=3))...)


## Sample from Imagesets, and check for similar images
imgset = sampleimageset(imgset_MG,imgset_UP;ps=[0.7,0.3],n=40000)
# imgset,lumimgset = excludesimilarimage!(imgset)
imgset,lumimgset = excludesimilarimage!(imgset;simfun=(i,j)->cor(vec(i),vec(j)))


## Imageset
imgsetname = "$(itype)_n$(length(imgset))_sizedeg$(sizedeg)_sizepx$(sizepx)"
imgsetdir = joinpath(stimuliroot,"ImageSets",imgsetname);mkpath(imgsetdir)
jldsave(joinpath(imgsetdir,"$imgsetname.jld2");imgset)


l = mapreduce(i->vec(i[:,:,3]),append!,imgset[1:30])
extrema(l)
l = mapreduce(i->vec(i[3,:,:]),append!,imgset_rgb[120:240])
extrema(l)

extrema(imgset_rgb[1])

density(vec(imgset_rgb[1][1,:,:]))

divmax=i->i/maximum(i)

colorview(RGB,divmax(imgset_rgb[12336]))
colorview(RGB,imgset_rgb[12336]/0.04)

extrema(imgset_rgb[12336])



imgset_UP = newimageset(joinpath(stimuliroot,idb),Regex("\\w*$itype.mat"),ppd;n=100,sizedeg,sizepx)

imgset_UP_rgb=map(i->tt(LMSToRGB,i),imgset_UP[1:100])
colorview(RGB,divmax(imgset_UP_rgb[51]))




maxlim = mapreduce(maximum,max,lumimgset)

qualim = mapreduce(i->quantile(vec(i),0.9),max,lumimgset)

newlumimgset = map(i->clamp.(i,0,qualim)/qualim,lumimgset)

using  ColorLab

colorview(Gray,lumimgset[12287])
colorview(Gray,newlumimgset[12287])

[0;0;;1;1]


RGBToLMS*[0.5,0.5,0.5]
RGBToXYZ*[0.5;0.5;0.5;;]
XYZ2xyY(RGBToXYZ*[0.5;0.5;0.5;;])
subimgset = imgset[1:50]
meanlms = mapreduce(i->mean(i,dims=(1,2)),(i,j)->(i.+j)/2,subimgset)

imgnormfun = (x;m=0,c=0.5,r=0.5)->begin
    xx = x.-m
    rr = maximum(abs.(xx),dims=(2,3))
    r*xx./rr .+ c
end

normimgset = map(i->imgnormfun(i,m=meanlum),subimgset)


colorview(RGB,normimgset[40])

# equal luminance
meanlumimgset = mapreduce(mean,mean,lumimgset)
sdrange=[0.23,0.33]
checkimageset!(imgset;sdrange)

imgsetname = "n$(length(imgset))_sizedeg$(sizedeg)_c$(c)_r$(r)_sd$(sdrange)"

saveunityrawtexture(joinpath(stimuliroot,imgsetname),imgset)
matwrite(joinpath(stimuliroot,"$imgsetname.mat"),Dict("imgset"=>imgset))
imgset = load(joinpath(stimuliroot,"$imgsetname.jld2"),"imgset")


t=(m,img)->mapslices(s->[m].*s,img,dims=(1,2))

b=t(LMSToRGB,imgset[1])

colordata = YAML.load_file(raw"C:\Users\fff00\Command\Data\ROGPG279Q\colordata.yaml")
LMSToRGB=reshape(colordata["LMSToRGB"],4,4)[1:3,1:3]
RGBToLMS=reshape(colordata["RGBToLMS"],4,4)[1:3,1:3]
RGBToXYZ=reshape(colordata["RGBToXYZ"],4,4)[1:3,1:3]

rand(3,10,10)
colorview(RGB,tt(LMSToRGB,imgset[1]))
colorview(RGB,eachslice(b,dims=1)...)

tt=(M,img)->reshape(M*reshape(permutedims(img,(3,1,2)),3,:),3,size(img)[1:2]...)

imgset_rgb=map(i->tt(LMSToRGB,i),imgset)

colorview(RGB,permutedims(imgset[27],(3,1,2)))
colorview(RGB,imgset_rgb[13505])




## Combine Imageset
normfun = (x;c=0.5,r=0.5,low=0.1,high=0.9)->begin
    xx = clamp.(x,quantile(vec(x),low),quantile(vec(x),high))
    xx .-= mean(xx)
    r*xx/maximum(abs.(xx)) .+ c
end
c=0.5 # mean luminance
r=0.5 # max extrema relative to c
imgset = combineimageset(imgset_MG,imgset_UP;ps=[0.7,0.3],n=40000,normfun=x->normfun(x;c,r))

timgset=imgset
@manipulate for ii in 1:length(timgset), ci in 1:size(timgset[1],3)
    t = timgset[ii][:,:,ci]
    ta = adjust_histogram(t, Equalization(nbins = 1024))
    ps,f1,f2 = powerspectrum2(t,sizepx[1]/sizedeg[1],freqrange=[-6,6])
    psa,f1,f2 = powerspectrum2(ta,sizepx[1]/sizedeg[1],freqrange=[-6,6])
    ps = log10.(ps);psa=log10.(psa);plims=(minimum([ps;psa]),maximum([ps;psa]))

    p = plot(layout=(3,2),leg=false,size=(2*300,3*300))
    heatmap!(p[1,1],t,color=:grays,aspect_ratio=1,frame=:none,yflip=true,clims=(0,1))
    histogram!(p[2,1],vec(t),xlims=[0,1],xticks=0:0.1:1);vline!(p[2,1],[mean(t),std(t)],lw=3,color=[:hotpink,:seagreen])

    heatmap!(p[1,2],ta,color=:grays,aspect_ratio=1,frame=:none,yflip=true,clims=(0,1))
    histogram!(p[2,2],vec(ta),xlims=[0,1],xticks=0:0.1:1);vline!(p[2,2],[mean(ta),std(ta)],lw=3,color=[:hotpink,:seagreen])

    heatmap!(p[3,1],f2,f1,ps,aspect_ratio=:equal,frame=:semi,color=:plasma,clims=plims)
    heatmap!(p[3,2],f2,f1,psa,aspect_ratio=:equal,frame=:semi,color=:plasma,clims=plims)
end

## Check and Save Imageset
sdrange=[0.23,0.33]
checkimageset!(imgset;sdrange)

imgsetname = "n$(length(imgset))_sizedeg$(sizedeg)_c$(c)_r$(r)_sd$(sdrange)"
save(joinpath(stimuliroot,"$imgsetname.jld2"),"imgset",imgset)
saveunityrawtexture(joinpath(stimuliroot,imgsetname),imgset)
matwrite(joinpath(stimuliroot,"$imgsetname.mat"),Dict("imgset"=>imgset))
imgset = load(joinpath(stimuliroot,"$imgsetname.jld2"),"imgset")
















## Gaussian Noise Imageset
gnormfun = x->begin
    xx = clamp.(x,-2.5,2.5)
    xx .-= mean(xx)
    0.5*xx/maximum(abs.(xx)) .+ 0.5
end
gimgset = [gnormfun(randn(sizepx)) for _ in 1:10000]

## Hartley Subspace Imageset
hs = hartleysubspace(kbegin=0.2,kend=5.0,dk=0.2,addhalfcycle=true,shape=:circle)
himgset = map(i -> begin
          ss = cas2sin(i...)
          grating(θ = ss.θ, sf = ss.f, phase = ss.phase, size = sizedeg, ppd = 30)
          end, hs)

## Imageset Mean Spectrum
pss,f1,f2 = powerspectrums2(imgset,sizepx[1]/sizedeg[1],freqrange=[-6,6])
gpss,gf1,gf2 = powerspectrums2(gimgset,sizepx[1]/sizedeg[1],freqrange=[-6,6])
hpss,hf1,hf2 = powerspectrums2(himgset,30,freqrange=[-6,6])

mps = log10.(reduce((i,j)->i.+j,pss))/length(pss)
gmps = log10.(reduce((i,j)->i.+j,gpss))/length(gpss)
hmps = log10.(reduce((i,j)->i.+j,hpss))/length(hpss)

mps[f1.==0,f2.==0].=minimum(mps)
gmps[gf1.==0,gf2.==0].=minimum(gmps)
hmps[hf1.==0,hf2.==0].=minimum(hmps)

plotmeanpowerspectrum = () -> begin
    p = plot(layout=(3,1),leg=false,size=(300,3*300))
    heatmap!(p[1],f2,f1,mps,aspect_ratio=:equal,frame=:none,color=:plasma,title="Natural Image")
    heatmap!(p[2],gf2,gf1,gmps,aspect_ratio=:equal,frame=:none,color=:plasma,title="Gaussian Noise")
    heatmap!(p[3],hf2,hf1,hmps,aspect_ratio=:equal,frame=:none,color=:plasma,title="Hartley Subspace")
    p
end

plotmeanpowerspectrum()
foreach(ext->savefig(joinpath(@__DIR__,"ImageSets_Mean_PowerSpectrum.$ext")),[".png",".svg"])












