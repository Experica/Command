using NeuroAnalysis,YAML,Interact

cd(@__DIR__)
## Generate hartley subspace for experica
dk=0.2
kbegin=0.2
kend=6.6

# add 4 uniform gray to match 4 phases of each grating with same average luminance
hs = hartleysubspace(kbegin=kbegin,kend=kend,dk=dk,phase=0,shape=:circle,addhalfcycle=true,blank=(kx=0,ky=0,phase=0.375),nblank=4)
ss = map(i->cas2sin(i...),hs)

# check gratings
@manipulate for i in 1:length(ss)
    Gray.(grating(θ=ss[i].θ,sf=ss[i].f,phase=ss[i].phase,sized=(5,5),ppd=30))
end

# save condition
hartleyconditions = Dict(:Ori=>map(i->i.θ,ss),:SpatialFreq=>map(i->i.f,ss),:SpatialPhase=>map(i->i.phase,ss))
title = "Hartley_k[$kbegin,$kend]_dk[$dk]"
YAML.write_file("$title.yaml",hartleyconditions)
