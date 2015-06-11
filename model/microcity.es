// Inital position
{ x -3 y -3 color #0099FF } mat
 
rule mat {
6 * { x 1 hue 20 } 6 * { y 1 hue 20 } split
// Ground plane
//{ s 10 10 0.01 color #FFFFFF x 0.2 y 0.2 z -9 } box
}

// X-split
rule split w 5 maxdepth 4 > square {
{ s 1/3 1 1.2 x -1 hue 5 z 0.01} split
{ s 2/3 1 1.2 x 1/4 hue 30 z 0.01 } split
}

// Y-split
rule split w 5 maxdepth 4 > square {
{ s 1 1/3 1.2 y -1 hue 10 z 0.01 } split
{ s 1 2/3 1.2 y 1/4 hue 20 z 0.01 } split
}

// No split
rule split { square }

rule square {
{ s 0.95 0.95 0.1 } box
}
 