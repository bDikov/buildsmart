# Architecture Specification: Unified Cloudflare R2 Media Management System

## 1. Executive Summary & Goals

Currently, media uploading across the BuildSmart platform is fragmented with duplicated logic across several features:
- **Landing Page CMS**: Direct R2 pre-signed uploads + fallback controller for hero and gallery items.
- **Tradesman Feed / Video Manager**: Pre-signed uploads with Hangfire FFmpeg background video processing (`VideoProcessingJob`).
- **Tradesman Portfolio & Certifications**: Multipart form POST to local/media storage.
- **Admin Category & SKU Icons**: Manual file selection without folder organization.

### The Objective
Consolidate all media storage, uploads, transformations, and folder organization into a **Unified Cloudflare R2 Media Management System** that allows:
1. **Folder-Based R2 Organization**: Grouping assets in logical folders (e.g. `/landing-pages/{slug}/`, `/feed/{tradesmanId}/`, `/categories/`, `/portfolios/`).
2. **Unified Media Processing & Optimization**:
   - **Images**: Automatic WebP compression, EXIF orientation correction, responsive scaling (Thumbnail, Medium 1080p, Full 1920p).
   - **Videos**: Background 720p/1080p MP4 transcoding, AAC audio, `+faststart` streaming optimization, and 1-second cover poster extraction via FFmpeg.
3. **Single Source of Truth Database Model**: Unified `MediaAsset` and `MediaFolder` entities in PostgreSQL.
4. **Reusable UI Components**:
   - `/admin/media-library`: Full-featured File Explorer (create folders, move, rename, upload, view metadata, delete).
   - `<MediaPickerModal>`: Reusable modal dialog with folder browsing and single-click selection for any form (Landing CMS, Spider-Net, Feed, Tradesman Profile).

---

## 2. Target Database Schema

### `MediaFolders` Table
```sql
CREATE TABLE "MediaFolders" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ParentId" UUID REFERENCES "MediaFolders"("Id") ON DELETE CASCADE,
    "Name" VARCHAR(100) NOT NULL,
    "Slug" VARCHAR(100) NOT NULL,
    "FullPath" VARCHAR(500) NOT NULL, -- e.g. "/landing-pages/remont-na-banya"
    "IsSystem" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX "IX_MediaFolders_ParentId_Slug" ON "MediaFolders" ("ParentId", "Slug");
CREATE INDEX "IX_MediaFolders_FullPath" ON "MediaFolders" ("FullPath");
```

### `MediaAssets` Table
```sql
CREATE TABLE "MediaAssets" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "FolderId" UUID REFERENCES "MediaFolders"("Id") ON DELETE SET NULL,
    "FileName" VARCHAR(255) NOT NULL,
    "R2Key" VARCHAR(500) NOT NULL UNIQUE, -- e.g. "landing-pages/remont-na-banya/hero_123.webp"
    "PublicUrl" VARCHAR(1000) NOT NULL,   -- e.g. "https://pub-...r2.dev/landing-pages/remont-na-banya/hero_123.webp"
    "ThumbnailUrl" VARCHAR(1000),         -- Scaled 320px thumbnail or extracted video poster
    "MediaType" VARCHAR(20) NOT NULL,     -- 'image' | 'video' | 'document'
    "ContentType" VARCHAR(100) NOT NULL,  -- 'image/webp', 'video/mp4', etc.
    "SizeBytes" BIGINT NOT NULL,
    "Width" INT,
    "Height" INT,
    "DurationSeconds" DOUBLE PRECISION,   -- for videos
    "AltTextBg" VARCHAR(255),
    "AltTextEn" VARCHAR(255),
    "UploaderUserId" UUID REFERENCES "Users"("Id") ON DELETE SET NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_MediaAssets_FolderId" ON "MediaAssets" ("FolderId");
CREATE INDEX "IX_MediaAssets_MediaType" ON "MediaAssets" ("MediaType");
```

---

## 3. R2 Folder & Key Naming Conventions

All assets in Cloudflare R2 bucket (`pub-7c580f96b011420fb7163dccc8051790.r2.dev`) will follow standard clean key prefixes:
- **Landing Pages**: `landing-pages/{slug}/{assetId}_{filename}.webp` (or `.mp4`)
- **Feed / Tradesman Media**: `feed/{tradesmanId}/{assetId}_{resolution}.mp4`
- **Portfolios**: `portfolios/{tradesmanId}/{assetId}.webp`
- **Certifications**: `certifications/{tradesmanId}/{assetId}_{filename}`
- **System / Spider-Net**: `categories/{categoryId}/icon.webp`
- **General / Unsorted**: `general/{yyyy}/{mm}/{assetId}_{filename}.webp`

---

## 4. Media Processing Pipeline

### A. Image Processing (`IImageProcessingService`)
Using `SixLabors.ImageSharp` or `SkiaSharp`:
1. **Validation & Orientation**: Read EXIF metadata, auto-rotate according to camera orientation.
2. **Compression & Conversion**: Convert PNG/JPEG/TIFF/BMP to modern **WebP** (Quality: 85) or **AVIF**.
3. **Dimension Normalization**:
   - **Full High-Res**: Max width 1920px (preserving aspect ratio).
   - **Thumbnail**: Max width 360px (for fast grid/admin previews).
4. **Upload to R2**: Upload WebP variant directly to R2 under the designated folder key prefix.

### B. Video Processing (`VideoProcessingJob`)
Consolidate the existing Hangfire FFmpeg worker:
1. Generate **Mobile 720p** (`-crf 28`, AAC audio, `+faststart`).
2. Generate **Desktop 1080p** (`-crf 23`, AAC audio, `+faststart`).
3. Extract **Poster Frame** at 1.0s and compress as WebP thumbnail.
4. Update `MediaAsset` record with mobile/desktop/thumbnail variants.

---

## 5. GraphQL API Design

### Queries
```graphql
query GetMediaFolders($parentId: UUID) {
  mediaFolders(parentId: $parentId) {
    id
    name
    slug
    fullPath
    parentId
    itemCount
    createdAt
  }
}

query GetMediaAssets($folderId: UUID, $mediaType: String, $searchTerm: String, $skip: Int, $take: Int) {
  mediaAssets(folderId: $folderId, mediaType: $mediaType, searchTerm: $searchTerm, skip: $skip, take: $take) {
    totalCount
    items {
      id
      fileName
      publicUrl
      thumbnailUrl
      mediaType
      contentType
      sizeBytes
      width
      height
      durationSeconds
      altTextBg
      altTextEn
      createdAt
    }
  }
}
```

### Mutations
```graphql
mutation CreateMediaFolder($name: String!, $parentId: UUID) {
  createMediaFolder(name: $name, parentId: $parentId) {
    id
    name
    fullPath
  }
}

mutation DeleteMediaFolder($id: UUID!) {
  deleteMediaFolder(id: $id)
}

mutation RequestFolderUploadUrl($folderPath: String!, $fileName: String!, $contentType: String!) {
  requestFolderUploadUrl(folderPath: $folderPath, fileName: $fileName, contentType: $contentType)
}

mutation RegisterUploadedMedia($folderId: UUID, $r2Key: String!, $fileName: String!, $contentType: String!, $sizeBytes: Long!) {
  registerUploadedMedia(folderId: $folderId, r2Key: $r2Key, fileName: $fileName, contentType: $contentType, sizeBytes: $sizeBytes) {
    id
    publicUrl
    thumbnailUrl
    mediaType
  }
}

mutation DeleteMediaAsset($id: UUID!) {
  deleteMediaAsset(id: $id)
}
```

---

## 6. Frontend UI Components (`BuildSmart.SharedUI`)

### 1. Unified Admin Media Explorer (`/admin/media-library`)
- **Folder Navigation Sidebar / Breadcrumb Bar**: Shows directory tree `/landing-pages/remont-na-banya/`.
- **Drag & Drop Upload Zone**: Multi-file batch upload with progress bar.
- **Action Toolbar**: "+ New Folder", "Upload Media", "Search", "Filter by Type (All / Images / Videos)", "Sort".
- **Asset Grid / List View**: Shows thumbnail, dimensions, file size, format tag, and copy URL button.
- **Asset Details Pane**: Rename, edit Alt text (BG/EN), move to folder, delete from R2 & DB.

### 2. Reusable `<MediaPickerModal>` Component
Parameters:
- `[Parameter] public bool IsOpen { get; set; }`
- `[Parameter] public string? InitialFolder { get; set; }` (e.g. `"landing-pages/remont-na-banya"`)
- `[Parameter] public string AllowedType { get; set; } = "all"` (`"image"`, `"video"`, `"all"`)
- `[Parameter] public EventCallback<MediaAssetDto> OnAssetSelected { get; set; }`
- `[Parameter] public EventCallback OnClose { get; set; }`

---

## 7. Migration & Rollout Strategy

1. **Phase 1: DB Migration & Service Layer**
   - Create EF Core migration for `MediaFolders` and `MediaAssets`.
   - Implement `IUnifiedMediaService` in `BuildSmart.Infrastructure` with folder path builders and R2 key management.
2. **Phase 2: GraphQL & Background Jobs**
   - Add GraphQL queries and mutations to `Query.cs` and `Mutation.cs`.
   - Seed default root folders (`/landing-pages/`, `/feed/`, `/categories/`, `/portfolios/`, `/general/`).
3. **Phase 3: Shared UI Components**
   - Create `<MediaPickerModal>` and `<MediaFolderExplorer>`.
   - Update `LandingPageCmsManager.razor`, `AdminVideoUpload.razor`, and `SpiderNetManager.razor` to use `<MediaPickerModal>`.
4. **Phase 4: Automatic Image Compression Pipeline**
   - Add background image optimization (WebP convert, max 1920px resize).
