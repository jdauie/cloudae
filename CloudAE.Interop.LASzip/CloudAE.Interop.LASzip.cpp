#include "stdafx.h"

#include <msclr/marshal_cppstd.h>

#include "CloudAE.Interop.LASzip.h"

#include <iostream>
#include <fstream>

using namespace CloudAE::Interop::LAZ;

// the decoder can be initialized with point format/size instead of vlr
// I don't know whether that works in general
LAZInterop::LAZInterop(System::String^ path, unsigned long dataOffset, array<Byte>^ vlr) {

	msclr::interop::marshal_context context;
	const char* pathStr = context.marshal_as<const char*>(path);
	
	m_stream = new ifstream(pathStr, ios::in | ios::binary);

	if (!m_stream->is_open())
		throw gcnew System::Exception("Unable to open");

	m_pointDataOffset = dataOffset;

	m_stream->seekg(dataOffset, ios::beg);

	m_zip = new LASzip();

	cli::pin_ptr<unsigned char> pVLR = &vlr[0];
	if (!m_zip->unpack(pVLR, vlr->Length))
		throw gcnew System::Exception("Unable to unpack() LAZ VLR");
	
	m_unzipper = new LASunzipper();
	if (!m_unzipper->open(*m_stream, m_zip))
		throw gcnew System::Exception("Unable to open() unzipper");

	// compute the point size
	m_lz_point_size = 0;
	for (unsigned int i = 0; i < m_zip->num_items; i++)
		m_lz_point_size += m_zip->items[i].size;

	// create the point data
	unsigned int point_offset = 0;
	m_lz_point = new unsigned char*[m_zip->num_items];
    
	m_lz_point_data = new unsigned char[m_lz_point_size];
	for (unsigned i = 0; i < m_zip->num_items; i++)
	{
		m_lz_point[i] = &(m_lz_point_data[point_offset]);
		point_offset += m_zip->items[i].size;
	}

	m_pointIndex = 0;
}

void LAZInterop::Seek(long long byteOffset) {
	
	long long byteOffsetIntoPointData = (byteOffset - m_pointDataOffset);
	long long pointOffset = (byteOffsetIntoPointData / m_lz_point_size);

	if (pointOffset != m_pointIndex) {
		m_unzipper->seek(pointOffset);
		m_pointIndex = pointOffset;
	}
}

int LAZInterop::Read(array<Byte>^ buffer, int byteOffset, int byteCount) {
	long pointCount = (byteCount / m_lz_point_size);
	int offset = byteOffset;

	for (int i = 0; i < pointCount; i++)
	{
		if (!m_unzipper->read(m_lz_point))
		{
			break;
		}

		for (int j = 0; j < m_lz_point_size; j++)
		{
			buffer[offset] = m_lz_point_data[j];
			++offset;
		}
		++m_pointIndex;
	}

	return (offset - byteOffset);
}

long long LAZInterop::GetPosition() {
	
	return (m_pointIndex * m_lz_point_size + m_pointDataOffset);
}

LAZInterop::~LAZInterop() {
	
	m_unzipper->close();

	m_stream->close();
	delete m_stream;

	delete[] m_lz_point;
	delete[] m_lz_point_data;
}
