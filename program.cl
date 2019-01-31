// #define GLINTEROP


uint GetBit( int x, int y, uint pw, __global uint* second ) { return (second[y * pw + (x >> 5)] >> (int)(x & 31)) & 1U; }
void BitSet( int x, int y, uint pw, __global uint* pattern) { atomic_or(&pattern[y * pw + (x >> 5)],1U << (int)(x & 31)); }
//void UnBitSet( int x, int y, uint pw, __global uint* pattern) { pattern[y * pw + (x >> 5)] |= 0U << (int)(x & 31); }

#ifdef GLINTEROP
__kernel void device_function( write_only image2d_t a, uint pw, uint h)
#else
__kernel void device_function( __global uint* p, __global uint* s, uint pw, uint h)
#endif
{
	int idx = get_global_id( 0 );
	int idy = get_global_id( 1 );
	uint n = 0;

	n = GetBit( idx - 1, idy - 1, pw, s) + GetBit( idx, idy - 1, pw, s) + GetBit( idx + 1, idy - 1, pw, s) + GetBit( idx - 1, idy, pw, s) +
		GetBit( idx + 1, idy, pw, s) + GetBit( idx - 1, idy + 1, pw, s) + GetBit( idx, idy + 1, pw, s) + GetBit( idx + 1, idy + 1, pw, s);
    if ((GetBit( idx, idy, pw, s) == 1 && n ==2) || n == 3) BitSet( idx, idy, pw, p);
	



	//C# code:
	//uint n = GetBit( x - 1, y - 1 ) + GetBit( x, y - 1 ) + GetBit( x + 1, y - 1 ) + GetBit( x - 1, y ) +
    //                GetBit( x + 1, y ) + GetBit( x - 1, y + 1 ) + GetBit( x, y + 1 ) + GetBit( x + 1, y + 1 );
    //            if ((GetBit( x, y ) == 1 && n ==2) || n == 3) BitSet( x, y );
	// helper function for setting one bit in the pattern buffer

    //    void BitSet( uint x, uint y ) { pattern[y * pw + (x >> 5)] |= 1U << (int)(x & 31); }
    // helper function for getting one bit from the secondary pattern buffer
    //    uint GetBit( uint x, uint y ) { return (second[y * pw + (x >> 5)] >> (int)(x & 31)) & 1U; }



}

