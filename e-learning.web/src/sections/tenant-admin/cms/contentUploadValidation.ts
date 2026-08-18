export const MAX_VIDEO_SIZE_BYTES = 2 * 1024 * 1024 * 1024;
export const MAX_DOCUMENT_SIZE_BYTES = 100 * 1024 * 1024;
const VIDEO_EXTENSIONS = ['.3gp', '.3g2', '.avi', '.flv', '.m4v', '.mkv', '.mov', '.mp4', '.mpeg', '.mpg', '.ogv', '.webm', '.wmv'];

type UploadValidationResult = {
  isValid: boolean;
  maxBytes: number;
  message: string;
  type: 'video' | 'document';
};

type UploadFileLike = Pick<File, 'name' | 'type' | 'size'> | null | undefined;

export function isVideoFile(file: UploadFileLike): boolean {
  const name = (file?.name || '').toLowerCase();
  const type = (file?.type || '').toLowerCase();

  return type.includes('video') || VIDEO_EXTENSIONS.some((extension) => name.endsWith(extension));
}

export function getUploadValidation(file: UploadFileLike): UploadValidationResult {
  if (!file) {
    return {
      isValid: true,
      maxBytes: MAX_DOCUMENT_SIZE_BYTES,
      message: '',
      type: 'document',
    };
  }

  if (isVideoFile(file)) {
    const isValid = file.size <= MAX_VIDEO_SIZE_BYTES;

    return {
      isValid,
      maxBytes: MAX_VIDEO_SIZE_BYTES,
      message: isValid ? '' : 'Dung lượng tệp video vượt quá giới hạn cho phép là 2GB.',
      type: 'video',
    };
  }

  const isValid = file.size <= MAX_DOCUMENT_SIZE_BYTES;

  return {
    isValid,
    maxBytes: MAX_DOCUMENT_SIZE_BYTES,
    message: isValid ? '' : 'Dung lượng tệp tài liệu vượt quá giới hạn cho phép là 100MB.',
    type: 'document',
  };
}