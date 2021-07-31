srcPath = ./Program.cs

outDir = ./out
outFile = $(outDir)/p2gif.exe

$(outFile): $(srcPath) | $(outDir)
	csc $(srcPath) -o -out:$(outFile)

run: $(outFile)
	mono $(outFile)

$(outDir):
	mkdir -p $@
