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

	LAZBlockReader* m_blockReader;

};

}}}
