#include "LAZBlockReader.h"

LAZBlockReader::LAZBlockReader(LASunzipper* unzipper, unsigned char** lz_point, unsigned char* lz_point_data, unsigned int lz_point_size) {
	m_unzipper      = unzipper;
	m_lz_point      = lz_point;
	m_lz_point_data = lz_point_data;
	m_lz_point_size = lz_point_size;
}

int LAZBlockReader::Read(unsigned char* buffer, int byteOffset, int byteCount) {
	
	unsigned char* bufferStart = buffer + byteOffset;
	unsigned char* bufferEnd = bufferStart + byteCount;
	unsigned char* bufferCurrent = bufferStart;

	while (bufferCurrent < bufferEnd)
	{
		if (!m_unzipper->read(m_lz_point))
			break;

		for (int j = 0; j < m_lz_point_size; j++)
		{
			*bufferCurrent = m_lz_point_data[j];
			++bufferCurrent;
		}
	}

	return (bufferCurrent - bufferStart);
}

LAZBlockReader::~LAZBlockReader() {
}
