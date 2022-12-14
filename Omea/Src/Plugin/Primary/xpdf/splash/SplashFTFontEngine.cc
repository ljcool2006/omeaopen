//========================================================================
//
// SplashFTFontEngine.cc
//
//========================================================================

#include <aconf.h>

#if HAVE_FREETYPE_FREETYPE_H || HAVE_FREETYPE_H

#ifdef USE_GCC_PRAGMAS
#pragma implementation
#endif

#include <stdio.h>
#ifndef WIN32
#  include <unistd.h>
#endif
#include "gmem.h"
#include "GString.h"
#include "gfile.h"
#include "FoFiTrueType.h"
#include "FoFiType1C.h"
#include "SplashFTFontFile.h"
#include "SplashFTFontEngine.h"

#ifdef VMS
#if (__VMS_VER < 70000000)
extern "C" int unlink(char *filename);
#endif
#endif

//------------------------------------------------------------------------

static void fileWrite(void *stream, char *data, int len) {
  fwrite(data, 1, len, (FILE *)stream);
}

//------------------------------------------------------------------------
// SplashFTFontEngine
//------------------------------------------------------------------------

SplashFTFontEngine::SplashFTFontEngine(GBool aaA, FT_Library libA) {
  aa = aaA;
  lib = libA;
}

SplashFTFontEngine *SplashFTFontEngine::init(GBool aaA) {
  FT_Library libA;

  if (FT_Init_FreeType(&libA)) {
    return NULL;
  }
  return new SplashFTFontEngine(aaA, libA);
}

SplashFTFontEngine::~SplashFTFontEngine() {
  FT_Done_FreeType(lib);
}

SplashFontFile *SplashFTFontEngine::loadType1Font(SplashFontFileID *idA,
						  char *fileName,
						  GBool deleteFile,
						  char **enc) {
  return SplashFTFontFile::loadType1Font(this, idA, fileName, deleteFile, enc);
}

SplashFontFile *SplashFTFontEngine::loadType1CFont(SplashFontFileID *idA,
						   char *fileName,
						   GBool deleteFile,
						   char **enc) {
  return SplashFTFontFile::loadType1Font(this, idA, fileName, deleteFile, enc);
}

SplashFontFile *SplashFTFontEngine::loadCIDFont(SplashFontFileID *idA,
						char *fileName,
						GBool deleteFile) {
  FoFiType1C *ff;
  Gushort *cidToGIDMap;
  int nCIDs;
  SplashFontFile *ret;

  // check for a CFF font
  if ((ff = FoFiType1C::load(fileName))) {
    cidToGIDMap = ff->getCIDToGIDMap(&nCIDs);
    delete ff;
  } else {
    cidToGIDMap = NULL;
    nCIDs = 0;
  }
  ret = SplashFTFontFile::loadCIDFont(this, idA, fileName, deleteFile,
				      cidToGIDMap, nCIDs);
  if (!ret) {
    gfree(cidToGIDMap);
  }
  return ret;
}

SplashFontFile *SplashFTFontEngine::loadTrueTypeFont(SplashFontFileID *idA,
						     char *fileName,
						     GBool deleteFile,
						     Gushort *codeToGID,
						     int codeToGIDLen) {
  FoFiTrueType *ff;
  GString *tmpFileName;
  FILE *tmpFile;
  SplashFontFile *ret;

  if (!(ff = FoFiTrueType::load(fileName))) {
    return NULL;
  }
  tmpFileName = NULL;
  if (!openTempFile(&tmpFileName, &tmpFile, "wb", NULL)) {
    delete ff;
    return NULL;
  }
  ff->writeTTF(&fileWrite, tmpFile);
  delete ff;
  fclose(tmpFile);
  ret = SplashFTFontFile::loadTrueTypeFont(this, idA,
					   tmpFileName->getCString(),
					   gTrue, codeToGID, codeToGIDLen);
  if (ret) {
    if (deleteFile) {
      unlink(fileName);
    }
  } else {
    unlink(tmpFileName->getCString());
  }
  delete tmpFileName;
  return ret;
}

#endif // HAVE_FREETYPE_FREETYPE_H || HAVE_FREETYPE_H
