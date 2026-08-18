import { useState, ReactNode, useEffect } from 'react';
// next
import { useRouter } from 'next/router';
// hooks
import useAuth from '../hooks/useAuth';
import { PATH_AUTH } from '../routes/paths';
// utils
import { saveReturnTo } from '../utils/cacheStorage';
import { switchSubdomain, extractSubdomainFromDomain } from '../utils/domainRouting';
import { setSession } from '../utils/jwt';
// redux
import { dispatch as reduxDispatch } from '@/redux/store';
import { clearAuthState } from '@/redux/slices/auth';
import { clearBranding } from '@/redux/slices/tenantBranding';
// components
import LoadingScreen from '../components/LoadingScreen';

// ----------------------------------------------------------------------

type Props = {
  children: ReactNode;
};

/**
 * Extracts the current browser subdomain relative to ROOT_DOMAIN.
 * Returns null if on the root domain itself, plain localhost, or on an admin subdomain.
 */
function getCurrentSubdomain(): string | null {
  if (typeof window === 'undefined') return null;
  return extractSubdomainFromDomain(window.location.hostname);
}

/** System / admin subdomains that should never trigger a tenant mismatch logout. */
const SYSTEM_SUBDOMAINS = new Set(['id', 'lms-admin', 'admin', 'www']);

export default function AuthGuard({ children }: Props) {
  const { isAuthenticated, isInitialized, user } = useAuth();

  const { asPath, pathname, push } = useRouter();

  const [requestedLocation, setRequestedLocation] = useState<string | null>(null);

  // ── Subdomain mismatch guard ────────────────────────────────────────────────
  // If any tenant-scoped user (CLIENT, SCHOOL, TEACHER, TENANT_ADMIN) tries to
  // access a different tenant's subdomain, force-logout immediately.
  useEffect(() => {
    if (!isInitialized || !isAuthenticated || typeof window === 'undefined') return;

    const role = (user?.role ?? '').toUpperCase().trim();

    // Only SUPER_ADMIN and LMS_ADMIN are allowed to roam across all tenants
    const isSystemAdmin = role === 'SUPER_ADMIN' || role === 'LMS_ADMIN';
    if (isSystemAdmin) return;

    const jwtSubdomain = (user as any)?.subdomain as string | null | undefined;

    const currentSubdomain = getCurrentSubdomain();

    // Debug log (always visible in dev – remove after confirming it works)
    if (process.env.NODE_ENV !== 'production') {
      console.log('[AuthGuard] Subdomain check →', {
        role,
        jwtSubdomain: jwtSubdomain ?? '(none)',
        currentSubdomain: currentSubdomain ?? '(none)',
        hostname: window.location.hostname,
      });
    }

    if (!jwtSubdomain) return; // no tenant bound in token – skip

    // If we are on plain localhost or a system subdomain, skip the check
    if (!currentSubdomain || SYSTEM_SUBDOMAINS.has(currentSubdomain.toLowerCase())) return;

    // Compare (case-insensitive)
    if (currentSubdomain.toLowerCase() === jwtSubdomain.toLowerCase()) return;

    // ── Mismatch detected ─────────────────────────────────────────────────────
    console.warn(
      `[AuthGuard] Subdomain mismatch: token belongs to "${jwtSubdomain}" but current host is "${currentSubdomain}". Forcing logout.`
    );

    // Clear session immediately (synchronous, no API call needed)
    setSession(null);
    reduxDispatch(clearAuthState());
    reduxDispatch(clearBranding());

    // Redirect to login page
    const isClientRoute = window.location.pathname.startsWith('/client');
    const loginUrl = isClientRoute 
      ? `${window.location.origin}${PATH_AUTH.login}` // Ở lại subdomain hiện tại
      : switchSubdomain('id', PATH_AUTH.login);       // Đẩy về trang ID cho quản lý
    window.location.replace(loginUrl);
  }, [isInitialized, isAuthenticated, user]);

  // ── Standard unauthenticated redirect ───────────────────────────────────────
  useEffect(() => {
    if (requestedLocation && pathname !== requestedLocation) {
      push(requestedLocation);
    }
    if (isAuthenticated) {
      setRequestedLocation(null);
    }
  }, [isAuthenticated, pathname, push, requestedLocation]);

  useEffect(() => {
    if (!isInitialized || isAuthenticated || typeof window === 'undefined') {
      return;
    }

    const requestedUrl = `${window.location.origin}${asPath}`;

    const isClientRoute = pathname.startsWith('/client');

    let loginUrl = '';

    if (isClientRoute) {
      // Nếu là Client: Đăng nhập ngay tại subdomain hiện tại
      // Ví dụ: https://stem-robot.daihoc.io.vn/auth/login
      loginUrl = `${window.location.origin}${PATH_AUTH.login}`;
    } else {
      // Nếu là các cấp quản lý khác: Đẩy về subdomain 'id'
      loginUrl = switchSubdomain('id', PATH_AUTH.login);
    }

    // On plain 'localhost', subdomains cannot share cookies.
    // Fallback to query param in that case to avoid redirect loops.
    const isPlainLocalhost = window.location.hostname === 'localhost' || window.location.hostname.endsWith('.localhost');

    if (isPlainLocalhost) {
      const targetUrl = `${loginUrl}?returnTo=${encodeURIComponent(requestedUrl)}`;
      if (window.location.href !== targetUrl) {
        window.location.replace(targetUrl);
      }
    } else {
      saveReturnTo(requestedUrl);
      if (window.location.href !== loginUrl) {
        window.location.replace(loginUrl);
      }
    }
  }, [asPath, pathname, isAuthenticated, isInitialized]);

  if (!isInitialized) {
    return <LoadingScreen />;
  }

  if (!isAuthenticated) {
    if (pathname !== requestedLocation) {
      setRequestedLocation(pathname);
    }
    return <LoadingScreen />;
  }

  return <>{children}</>;
}
