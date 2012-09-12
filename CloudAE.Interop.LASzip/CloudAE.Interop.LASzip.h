#pragma once

#include <stdio.h>

#include "lasunzipper.hpp"

using namespace System;

namespace CloudAE { namespace Interop { namespace LAZ {

public ref class LAZInterop
{
public:

	LAZInterop(System::String^ path, unsigned long dataOffset, array<Byte>^ vlr);
    ~LAZInterop();

	// provide a logical byte-based access (even though it is actually compressed)
	void Seek(long byteIndex);
	void Read(array<Byte>^ buffer, int byteOffset, int byteCount);

private:

	System::String^ m_path;
	array<Byte>^ m_lazVLR;
	unsigned long m_pointDataOffset;

	FILE* m_file;

	LASzip* m_zip;
	LASunzipper* m_unzipper;
	long m_pointIndex;

	unsigned char** m_lz_point;
	unsigned char* m_lz_point_data;
	unsigned int m_lz_point_size;

};

}}}
