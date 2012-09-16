#pragma once

#include <stdio.h>

#include "lasunzipper.hpp"
#include "LAZBlockReader.h"

using namespace System;

namespace CloudAE { namespace Interop { namespace LAZ {

public ref class LAZInterop
{
public:

	LAZInterop(System::String^ path, unsigned long dataOffset, array<Byte>^ vlr);
    ~LAZInterop();

	// provide a logical byte-based access (even though it is actually compressed)
	void Seek(long long byteIndex);
	int Read(array<Byte>^ buffer, int byteOffset, int byteCount);
	long long GetPosition();

private:

	System::String^ m_path;
	array<Byte>^ m_lazVLR;
	unsigned long m_pointDataOffset;

	ifstream* m_stream;

	LASzip* m_zip;
	LASunzipper* m_unzipper;
	long long m_pointIndex;

	unsigned char** m_lz_point;
	unsigned char* m_lz_point_data;
	unsigned int m_lz_point_size;

	LAZBlockReader* m_blockReader;

};

}}}
