-- V22__seed_content_items.sql

-- Insert some dummy content items for the STEM tenant ('00000000-0000-0000-0000-000000000002')
-- and the LESSON node 'LUỸ THỪA' ('04569ed1-7ee0-4549-924d-57f9a70d49ff')

INSERT INTO curriculum_node (id, tenant_id, node_type, title, sort_order)
VALUES ('04569ed1-7ee0-4549-924d-57f9a70d49ff', '00000000-0000-0000-0000-000000000002', 'LESSON', 'LUỸ THỪA', 1)
ON CONFLICT (id) DO NOTHING;

INSERT INTO content_item (
    id, tenant_id, curriculum_node_id, type, title, description, 
    source_url, publish_status, media_processing_status, is_downloadable
) VALUES (
    '00000000-0000-0000-0007-000000000001',
    '00000000-0000-0000-0000-000000000002',
    '04569ed1-7ee0-4549-924d-57f9a70d49ff',
    'URL',
    'Giới thiệu về Luỹ thừa (YouTube)',
    'Video giới thiệu cơ bản lấy từ YouTube',
    'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    'PUBLISHED',
    'NOT_REQUIRED',
    false
) ON CONFLICT DO NOTHING;

INSERT INTO content_item (
    id, tenant_id, curriculum_node_id, type, title, description, 
    file_name, file_path, publish_status, media_processing_status, is_downloadable
) VALUES (
    '00000000-0000-0000-0007-000000000002',
    '00000000-0000-0000-0000-000000000002',
    '04569ed1-7ee0-4549-924d-57f9a70d49ff',
    'PDF',
    'Tài liệu hướng dẫn học (PDF)',
    'Đây là tài liệu dạng PDF (file ảo để test UI)',
    'huong_dan_luy_thua.pdf',
    'mock/huong_dan_luy_thua.pdf',
    'PUBLISHED',
    'NOT_REQUIRED',
    true
) ON CONFLICT DO NOTHING;

INSERT INTO content_item (
    id, tenant_id, curriculum_node_id, type, title, description, 
    file_name, file_path, hls_url, publish_status, media_processing_status, is_downloadable
) VALUES (
    '00000000-0000-0000-0007-000000000003',
    '00000000-0000-0000-0000-000000000002',
    '04569ed1-7ee0-4549-924d-57f9a70d49ff',
    'VIDEO',
    'Video bài giảng chi tiết (HLS)',
    'Video bài giảng nội bộ đã qua xử lý (file ảo để test UI)',
    'bai_giang_chi_tiet.mp4',
    'mock/bai_giang_chi_tiet.mp4',
    'mock/hls/master.m3u8',
    'PUBLISHED',
    'READY',
    false
) ON CONFLICT DO NOTHING;
