using NeuroAnalysis,YAML,Plots,Interact

cd(@__DIR__)
## Generate hartley subspace
dk=0.2
kbegin=0.2
kend=6.6
# add 4 uniform gray as blanks to match 4 phases of each grating with the same mean luminance
nblank=4

hs = hartleysubspace(;kbegin,kend,dk,phase=0.0,shape=:circle,addhalfcycle=true,blank=(kx=0.0,ky=0.0,phase=0.375),nblank)
ss = map(i->cas2sin(i...),hs)

# check gratings
@manipulate for i in 1:length(ss)
    Gray.(grating(θ=ss[i].θ,sf=ss[i].f,phase=ss[i].phase,size=(5,5),ppd=30))
end

# save condition
hartleyconditions = Dict(:Ori=>map(i->rad2deg(mod(i.θ,2π)),ss),:SpatialFreq=>map(i->i.f,ss),:SpatialPhase=>map(i->i.phase,ss))
title = "Hartley_k[$kbegin,$kend]_dk[$dk]_nblank[$nblank]"
YAML.write_file("$title.yaml",hartleyconditions)
