import { useEffect, useRef, useState } from 'react';
import axios from '@/utils/axios';
import { getToken } from '@/utils/cacheStorage';
import { HEARTBEAT_INTERVAL_MS } from '@/config';

const INACTIVITY_THRESHOLD_MS = 60000; // 60s
const LS_KEY_PREFIX = 'learning_session_';
const RESUME_THRESHOLD_MS = 1800000; // 30 mins

function generateId(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  // Fallback
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
  });
}

export function useLearningSession(contentId: string | undefined) {
  const [learningSessionId, setLearningSessionId] = useState<string | null>(null);
  const lastActiveRef = useRef<number>(Date.now());
  const inactivityAccumRef = useRef<number>(0);
  const isEndingSessionRef = useRef<boolean>(false); // Tránh gọi endSession nhiều lần

  // 1. Lifecycle: start on contentId change
  useEffect(() => {
    if (!contentId) return;

    const sessionDataStr = localStorage.getItem(LS_KEY_PREFIX + contentId);
    let lsid = '';

    if (sessionDataStr) {
      try {
        const sessionData = JSON.parse(sessionDataStr);
        // Nếu thời gian rời đi chưa quá 30 phút, dùng lại session cũ
        if (Date.now() - sessionData.lastActive < RESUME_THRESHOLD_MS) {
          lsid = sessionData.id;
        }
      } catch (e) {
        console.error('Failed to parse session data', e);
      }
    }

    if (!lsid) {
      lsid = generateId();
    }

    localStorage.setItem(
      LS_KEY_PREFIX + contentId,
      JSON.stringify({ id: lsid, lastActive: Date.now() })
    );

    setLearningSessionId(lsid);
    lastActiveRef.current = Date.now();
    inactivityAccumRef.current = 0;
    isEndingSessionRef.current = false;

    // Start/Upsert session
    axios.post('/api/content/session/start', {
      learningSessionId: lsid,
      contentItemId: contentId,
      deviceInfo: { userAgent: navigator.userAgent, platform: 'web' },
    }).catch((err) => {
      console.error('Failed to start/upsert learning session', err);
    });

    // Cleanup function: end session on route change
    return () => {
      if (lsid) {
        const token = getToken();
        const headers: Record<string, string> = { 'Content-Type': 'application/json' };
        if (token) headers['Authorization'] = `Bearer ${token}`;

        fetch('/api/content/session/end', {
          method: 'PUT',
          headers,
          body: JSON.stringify({ learningSessionId: lsid }),
          keepalive: true,
        }).catch(() => {});
        
        localStorage.removeItem(LS_KEY_PREFIX + contentId);
      }
    };
  }, [contentId]);

  // 2. Long heartbeat (5 min) — server tự tính inactivity dựa trên gap last_update
  useEffect(() => {
    if (!learningSessionId) return;

    const interval = setInterval(() => {
      axios.put('/api/content/session/heartbeat', {
        learningSessionId,
      }).catch(() => {});
      lastActiveRef.current = Date.now();
    }, HEARTBEAT_INTERVAL_MS);

    return () => clearInterval(interval);
  }, [learningSessionId]);

  // 3. Track user activity & Page Exit
  useEffect(() => {
    const updateActivity = () => { lastActiveRef.current = Date.now(); };
    window.addEventListener('mousemove', updateActivity);
    window.addEventListener('keydown', updateActivity);
    window.addEventListener('scroll', updateActivity);
    window.addEventListener('touchstart', updateActivity);

    const handleExit = () => {
      if (!learningSessionId || isEndingSessionRef.current) return;
      isEndingSessionRef.current = true;

      const token = getToken();
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };
      if (token) headers['Authorization'] = `Bearer ${token}`;

      localStorage.setItem(
        LS_KEY_PREFIX + contentId,
        JSON.stringify({ id: learningSessionId, lastActive: Date.now() })
      );

      // Set null ngay lập tức để dừng heartbeat interval
      setLearningSessionId(null);

      fetch('/api/content/session/end', {
        method: 'PUT',
        headers,
        body: JSON.stringify({ learningSessionId }),
        keepalive: true,
      }).finally(() => {
        isEndingSessionRef.current = false;
      });
    };

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'hidden') {
        handleExit();
      } else if (document.visibilityState === 'visible' && contentId) {
        // Restart session khi quay lại tab
        const sessionDataStr = localStorage.getItem(LS_KEY_PREFIX + contentId);
        if (sessionDataStr) {
          try {
            const sessionData = JSON.parse(sessionDataStr);
            setLearningSessionId(sessionData.id); // Kích hoạt lại heartbeat
            axios.post('/api/content/session/start', {
              learningSessionId: sessionData.id,
              contentItemId: contentId,
              deviceInfo: { userAgent: navigator.userAgent, platform: 'web' },
            }).catch(() => {});
          } catch (e) {}
        }
      }
    };

    window.addEventListener('beforeunload', handleExit);
    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      window.removeEventListener('mousemove', updateActivity);
      window.removeEventListener('keydown', updateActivity);
      window.removeEventListener('scroll', updateActivity);
      window.removeEventListener('touchstart', updateActivity);
      window.removeEventListener('beforeunload', handleExit);
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [learningSessionId, contentId]); // Thêm contentId vào dependency array

  return { learningSessionId };
}
