#pragma once

#include "lasunzipper.hpp"

class LAZBlockReader
{
public:

	LAZBlockReader(const char* path, unsigned long dataOffset, unsigned char* vlr, unsigned int vlrLength);
    ~LAZBlockReader();

	int Read(unsigned char* buffer, int byteOffset, int byteCount);
	void Seek(long long byteOffset);
	long long GetPosition();

private:

	unsigned long m_pointDataOffset;
	long long m_pointIndex;

	char* m_streamBuffer;
	FILE* m_file;

	LASzip* m_zip;
	LASunzipper* m_unzipper;

	unsigned char** m_lz_point;
	unsigned char* m_lz_point_data;
	unsigned int m_lz_point_size;

};


