
uint GetBit( int x, int y, uint pw, __global uint* second ) { return (second[y * pw + (x >> 5)] >> (int)(x & 31)) & 1U; }
void BitSet( int x, int y, uint pw, __global uint* pattern) { atomic_or(&pattern[y * pw + (x >> 5)],1U << (int)(x & 31)); }


__kernel void wrap_mode( __global uint* p, __global uint* s, uint pw, uint h)
{

	int above;
	int below;
	int left;
	int right;
	int idx = get_global_id( 0 );
	int idy = get_global_id( 1 );
	uint n = 0;
	uint w = pw * 32;

	if(idy==0) above = h;
	else above = idy+1;
	if(idy==h) below < 0;
	else below = idy-1;
	if(idx==0) left = w;
	else left = idx-1;
	if(idx==w) right < 0;
	else right = idx+1;
	
	n = GetBit( left, below, pw, s) + GetBit( idx, below, pw, s) + GetBit( right, below, pw, s) + GetBit( left, idy, pw, s) +
		GetBit( right, idy, pw, s) + GetBit( left, above, pw, s) + GetBit( idx, above, pw, s) + GetBit( right, above, pw, s);
	
    if ((GetBit( idx, idy, pw, s) == 1 && n ==2) || n == 3) BitSet( idx, idy, pw, p);
	
}

__kernel void dead_mode( __global uint* p, __global uint* s, uint pw, uint h)
{

	int above;
	int below;
	int left;
	int right;
	int idx = get_global_id( 0 );
	int idy = get_global_id( 1 );
	uint n = 0;
	uint w = pw * 32;

	uint tl = GetBit( idx-1, idy+1, pw, s);
	uint tm = GetBit( idx  , idy+1, pw, s);
	uint tr = GetBit( idx+1, idy+1, pw, s);
	uint bl = GetBit( idx-1, idy-1, pw, s);
	uint bm = GetBit( idx  , idy-1, pw, s);
	uint br = GetBit( idx+1, idy-1, pw, s);
	uint ml = GetBit( idx-1, idy, pw, s);
	uint mr = GetBit( idx+1, idy, pw, s);

	
	n = tl + tm + tr + bl + bm + br + ml + mr;

    if(idx == 0) n -= tl + ml + bl;
	if(idx == w) n -= tr + mr + br;
	if(idy == 0) n -= bl + bm + br;
	if(idy == h) n -= tl + tm + tr;
	
    if ((GetBit( idx, idy, pw, s) == 1 && n ==2) || n == 3) BitSet( idx, idy, pw, p);
	
}

