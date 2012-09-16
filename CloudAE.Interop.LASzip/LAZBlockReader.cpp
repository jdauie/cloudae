#include "stdafx.h"

#include "LAZBlockReader.h"

LAZBlockReader::LAZBlockReader(LASunzipper* unzipper, unsigned char** lz_point, unsigned char* lz_point_data, unsigned int lz_point_size) {
	m_unzipper      = unzipper;
	m_lz_point      = lz_point;
	m_lz_point_data = lz_point_data;
	m_lz_point_size = lz_point_size;
}

int LAZBlockReader::Read(unsigned char* buffer, int byteOffset, int byteCount) {
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
		//++m_pointIndex;
	}

	return (offset - byteOffset);
}

LAZBlockReader::~LAZBlockReader() {
}
