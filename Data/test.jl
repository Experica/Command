rgbs = IsoLumc
luvs = Luv.(IsoLumc)
lchs = LCHuv.(IsoLumc)

using Test
ls = comp1.(luvs)
@test all(comp1.(luvs) .== comp1.(lchs))

scatter(comp2.(luvs),comp3.(luvs),color=rgbs,markersize=9)
scatter(hue.(lchs),comp2.(lchs),color=rgbs,markersize=9,proj=:polar)