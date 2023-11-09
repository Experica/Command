using FileIO,JLD2,MAT,NeuroAnalysis,Images,Interpolations,Random,StatsBase,StatsPlots,ProgressMeter,MsgPack,YAML,ColorLab

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
        lim = mapreduce(maximum,max,imgpatchs)
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

function excludesimilarimage!(imgset;alpha=0.7,lumfun=lumlms,simfun=MSSSIM(),n=nothing)
    di = []
    for i in eachindex(imgset)
        (any(isnan.(imgset[i])) || any(isinf.(imgset[i]))) && push!(di,i)
    end
    deleteat!(imgset,di)

    l=length(imgset);di=falses(l);lumimgset=lumfun.(imgset)
    lumlim = mapreduce(maximum,max,lumimgset)
    lumimgset = map(i->i/lumlim,lumimgset)
    @showprogress desc="Checking Images ... " for i in 1:(l-1)
        di[i] && continue
        Threads.@threads for j in (i+1):l
            di[j] && continue
            simfun(lumimgset[j],lumimgset[i]) > alpha && (di[j]=true)
        end
    end
    deleteat!(imgset,di);deleteat!(lumimgset,di)
    if !isnothing(n)
        imgset=imgset[1:n];lumimgset=lumimgset[1:n]
    end
    imgset,lumimgset
end

function normlum(x;m=0.5,mad=0.5,pc=(1,99))
    xx = clampscale(x,pc)
    xx .-= mean(xx)
    mad*xx/maximum(abs.(xx)) .+ m
end

function changespace(M,img)
    ds = size(img)
    if ds[1] == 3
        t = img;sdims=ds[2:3]
    else
        t=permutedims(img,(3,1,2));sdims=ds[1:2]
    end
    reshape(M*reshape(t,3,:),3,sdims...)
end

function msgpackunitytexture(filename,imgset;meancolor=nothing,eltype=UInt8)
    imgsize=size(imgset[1])
    # Unity Texture2D pixels unfold from left->right, then bottom->up
    if length(imgsize) == 2
        isnothing(meancolor) && (meancolor = mapreduce(mean,(i,j)->(i+j)/2,imgset))
        meancolor = fill(meancolor,3)
        ips = map(i->vec(reverse!(permutedims(i),dims=2)),imgset)
    else
        isnothing(meancolor) && (meancolor = mapreduce(i->dropdims(mean(i,dims=(2,3)),dims=(2,3)),(i,j)->(i.+j)/2,imgset))
        ips = map(i->vec(reverse!(permutedims(i,(1,3,2)),dims=3)),imgset)
    end
    if eltype==UInt8
        ips = map(i->reinterpret.(N0f8.(i)),ips)
    end
    data = Dict("ImageSize"=>[Int32.(imgsize)...],"Images"=>ips,"MeanColor"=>Float32.(meancolor))
    write("$filename.$eltype.mpis", pack(data))
end




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


## Sample from Imagesets, and check for similar images
imgset = sampleimageset(imgset_MG,imgset_UP;ps=[0.7,0.3],n=40000)
# imgset,lumimgset = excludesimilarimage!(imgset)
imgset,lumimgset = excludesimilarimage!(imgset;simfun=(i,j)->cor(vec(i),vec(j)))

## Imageset
imgsetname = "$(itype)_n$(length(imgset))_sizedeg$(sizedeg)_sizepx$(sizepx)"
imgsetdir = joinpath(stimuliroot,"ImageSets",imgsetname);mkpath(imgsetdir)
imgsetpath = joinpath(stimuliroot,"ImageSets","$imgsetname.jld2")
jldsave(imgsetpath;imgset)
imgset = load(imgsetpath,"imgset")




## LMS luminance clamp and scale in [0, 1], and mean luminance shifted to 0.5 to get balanced range
pc=(5,95)
imgset_lum = map(i->normlum(i;pc),lumimgset)

# Confine contrast range and save luminance imageset
sdrange=(0.2,0.35)
n=24000
imgset_lum_c = filter(i->begin
    sd = std(i)
    !isnan(sd) && sdrange[1]<sd<sdrange[2]
end,imgset_lum)[1:n]

colorview(Gray,imgset_lum_c[190])

lumimgsetname = "Lum_n$(length(imgset_lum_c))_sizedeg$(sizedeg)_sizepx$(sizepx)_pc$(pc)_sd$(sdrange)"
save(joinpath(imgsetdir,"$lumimgsetname.jld2"),"imgset",imgset_lum_c)
msgpackunitytexture(joinpath(imgsetdir,lumimgsetname),imgset_lum_c)




## L,M,S clamp and scale in [0, 1] to modulate Cones
pc=(1,99)
imgset_lms_cm = map(i->stack(s->clampscale(s,pc),eachslice(i,dims=3)),imgset)
filter!(i->!(any(isnan.(i)) || any(isinf.(i))),imgset_lms_cm)
imgset_lms_cm = imgset_lms_cm[1:24000]

colorview(LMS,eachslice(imgset_lms_cm[218],dims=3)...)

lmsimgsetname = "LMS_n$(length(imgset_lms_cm))_sizedeg$(sizedeg)_sizepx$(sizepx)_pc$(pc)_cm"
save(joinpath(imgsetdir,"$lmsimgsetname.jld2"),"imgset",imgset_lms_cm)
msgpackunitytexture(joinpath(imgsetdir,lmsimgsetname),map(i->permutedims(i,(3,1,2)),imgset_lms_cm))




## Prepare Imageset in RGB of the Presenting Display
displayname = "ROGPG279Q"
colordata = YAML.load_file(joinpath(@__DIR__,displayname,"colordata.yaml"))
LMSToRGB= dehomomatrix(reshape(colordata["LMSToRGB"],4,4))
RGBToLMS= dehomomatrix(reshape(colordata["RGBToLMS"],4,4))

imgset_rgb = map(i->changespace(LMSToRGB,i),imgset)
# The RGB color and luminance that had be captured by the camera of natural image database(converted to LMS)
# may not be reproduced on the presenting display, so we clamp and scale them into the display RGB space and luminance range.
pc=(1,99)
imgset_rgb_pc = map(i->clampscale(i,pc),imgset_rgb)
imgset_lms_pc = map(i->changespace(RGBToLMS,i),imgset_rgb_pc)

colorview(RGB,imgset_rgb_pc[193])

# Save
displayimgsetname = "$(displayname)_RGB_n$(length(imgset_rgb_pc))_sizedeg$(sizedeg)_sizepx$(sizepx)_pc$(pc)"
save(joinpath(imgsetdir,"$displayimgsetname.jld2"),"imgset",map(i->permutedims(Float32.(i),(2,3,1)),imgset_rgb_pc))

msgpackunitytexture(joinpath(imgsetdir,displayimgsetname),imgset_rgb_pc)

displayimgsetname = "$(displayname)_LMS_n$(length(imgset_rgb_pc))_sizedeg$(sizedeg)_sizepx$(sizepx)_pc$(pc)"
save(joinpath(imgsetdir,"$displayimgsetname.jld2"),"imgset",map(i->permutedims(Float32.(i),(2,3,1)),imgset_lms_pc))




## Try to move mean luminance to around 0.5, and maximize contrast
normcolor = (x;m=0.5,mad=0.5)->begin
    xx = x.-mean(x)
    mad*xx/maximum(abs.(xx)) .+ m
end
normcolor1 = (x;m=0.5,mad=0.5)->begin
    xx = x.-mean(x,dims=(2,3))
    mad*xx/maximum(abs.(xx)) .+ m
end
normcolor2 = (x;m=0.5,mad=0.5)->begin
    xx = x.-mean(x)
    mad*xx./maximum(abs.(xx),dims=(2,3)) .+ m
end
normcolor3 = (x;m=0.5,mad=0.5)->begin
    xx = x.-mean(x,dims=(2,3))
    mad*xx./maximum(abs.(xx),dims=(2,3)) .+ m
end

i = 195
plot(hcat(
colorview(RGB,imgset_rgb_pc[i]),
colorview(RGB,normcolor(imgset_rgb_pc[i])),
colorview(RGB,normcolor1(imgset_rgb_pc[i])),
colorview(RGB,normcolor2(imgset_rgb_pc[i])),
colorview(RGB,normcolor3(imgset_rgb_pc[i]))),
frame=:none,size=(1000,200))

p = "bl"
imgset_rgb_pc_p = map(i->normcolor(i),imgset_rgb_pc)
imgset_lms_pc_p = map(i->changespace(RGBToLMS,i),imgset_rgb_pc_p)

# Save
displayimgsetname = "$(displayname)_RGB_n$(length(imgset_rgb_pc_p))_sizedeg$(sizedeg)_sizepx$(sizepx)_pc$(pc)_$p"
save(joinpath(imgsetdir,"$displayimgsetname.jld2"),"imgset",map(i->permutedims(Float32.(i),(2,3,1)),imgset_rgb_pc_p))

msgpackunitytexture(joinpath(imgsetdir,displayimgsetname),imgset_rgb_pc_p)

displayimgsetname = "$(displayname)_LMS_n$(length(imgset_rgb_pc_p))_sizedeg$(sizedeg)_sizepx$(sizepx)_pc$(pc)_$p"
save(joinpath(imgsetdir,"$displayimgsetname.jld2"),"imgset",map(i->permutedims(Float32.(i),(2,3,1)),imgset_lms_pc_p))




## Gaussian Noise ImageSet
gnorm = x->begin
    xx = clamp.(x,-2.5,2.5)
    0.5*xx/2.5 .+ 0.5
end
sizepx = (32,32)
gimgset = [gnorm(randn(sizepx...,3)) for _ in 1:36000]

colorview(RGB,eachslice(gimgset[222],dims=3)...)

# Save
imgsetname = "RGB_n$(length(gimgset))_sizepx$(sizepx)_Gaussian"
imgsetdir = joinpath(stimuliroot,"ImageSets",imgsetname);mkpath(imgsetdir)
imgsetpath = joinpath(stimuliroot,"ImageSets","$imgsetname.jld2")
save(imgsetpath,"imgset",gimgset)
msgpackunitytexture(joinpath(imgsetdir,imgsetname),map(i->permutedims(i,(3,1,2)),gimgset))






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












