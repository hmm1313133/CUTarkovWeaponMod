import struct, sys, zlib

def read_png(path):
    with open(path, 'rb') as f:
        data = f.read()
    # parse chunks
    pos = 8
    width = height = None
    idat = b''
    while pos < len(data):
        length = struct.unpack('>I', data[pos:pos+4])[0]
        ctype = data[pos+4:pos+8]
        cdata = data[pos+8:pos+8+length]
        if ctype == b'IHDR':
            width, height, bitdepth, colortype = struct.unpack('>IIBB', cdata[:10])
        elif ctype == b'IDAT':
            idat += cdata
        pos += 12 + length
    raw = zlib.decompress(idat)
    # unfilter
    stride = width * 4
    out = bytearray()
    prev = bytearray(stride)
    bpp = 4
    for y in range(height):
        ft = raw[y*(stride+1)]
        line = bytearray(raw[y*(stride+1)+1:(y+1)*(stride+1)])
        if ft == 1:  # Sub
            for i in range(bpp, stride):
                line[i] = (line[i] + line[i-bpp]) & 0xFF
        elif ft == 2:  # Up
            for i in range(stride):
                line[i] = (line[i] + prev[i]) & 0xFF
        elif ft == 3:  # Average
            for i in range(stride):
                a = line[i-bpp] if i >= bpp else 0
                b = prev[i]
                line[i] = (line[i] + ((a + b) >> 1)) & 0xFF
        elif ft == 4:  # Paeth
            for i in range(stride):
                a = line[i-bpp] if i >= bpp else 0
                b = prev[i]
                c = prev[i-bpp] if i >= bpp else 0
                p = a + b - c
                pa, pb, pc = abs(p-a), abs(p-b), abs(p-c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pr) & 0xFF
        out += line
        prev = line
    return width, height, out

def analyze(path):
    w, h, px = read_png(path)
    # find bounding box of non-transparent pixels
    minx, miny, maxx, maxy = w, h, -1, -1
    for y in range(h):
        for x in range(w):
            a = px[(y*w+x)*4+3]
            if a > 0:
                if x < minx: minx = x
                if x > maxx: maxx = x
                if y < miny: miny = y
                if y > maxy: maxy = y
    print(f"{path.split('/')[-1]}: {w}x{h}, bbox=({minx},{miny})-({maxx},{maxy}), bbox_size={maxx-minx+1}x{maxy-miny+1}")

for p in sys.argv[1:]:
    analyze(p)
