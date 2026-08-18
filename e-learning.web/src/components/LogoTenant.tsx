import { forwardRef } from 'react';
import NextLink from 'next/link';

import { Box, BoxProps } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import useCollapseDrawer from '../hooks/useCollapseDrawer';
import useAuth from '../hooks/useAuth';
import { useSelector } from '../redux/store';

// ----------------------------------------------------------------------

interface Props extends BoxProps {
  disabledLink?: boolean;
  withText?: boolean;
  withColor?: boolean;
}

export const normalizeTenantBrandColor = (value?: string | null) => {
  if (typeof value !== 'string') return '';
  const trimmed = value.trim();
  if (!trimmed) return '';
  const normalized = trimmed.startsWith('#') ? trimmed : `#${trimmed}`;
  return normalized.length === 4
    ? `#${normalized[1]}${normalized[1]}${normalized[2]}${normalized[2]}${normalized[3]}${normalized[3]}`
    : normalized;
};

export const getTenantBrandContrastText = (value?: string | null) => {
  const normalized = normalizeTenantBrandColor(value);
  if (!normalized) return '#ffffff';

  const hex = normalized.replace('#', '');
  const safe = hex.length === 3 ? hex.split('').map((char) => `${char}${char}`).join('') : hex;
  const red = parseInt(safe.slice(0, 2), 16);
  const green = parseInt(safe.slice(2, 4), 16);
  const blue = parseInt(safe.slice(4, 6), 16);
  const brightness = (red * 299 + green * 587 + blue * 114) / 1000;

  return brightness > 160 ? '#111827' : '#ffffff';
};

export const getTenantWorkspaceBranding = (
  workspaces: any[] = [],
  hostname: string
) => {
  const candidates = [
    ...(Array.isArray(workspaces) ? workspaces : []),
  ].filter(Boolean);

  if (!candidates.length) return null;

  const normalizedHostname = hostname?.toLowerCase();
  if (normalizedHostname) {
    const matchedWorkspace = candidates.find((workspace: any) => {
      const domain = `${workspace?.domain || ''}`.toLowerCase();
      const subdomain = `${workspace?.subdomain || ''}`.toLowerCase();
      return Boolean(
        (domain && (normalizedHostname === domain || normalizedHostname.endsWith(`.${domain}`))) ||
          (subdomain &&
            (normalizedHostname === subdomain ||
              normalizedHostname.endsWith(`.${subdomain}`) ||
              normalizedHostname.startsWith(`${subdomain}.`)))
      );
    });

    if (matchedWorkspace) return matchedWorkspace;
  }

  return candidates[0];
};

const LogoTenant = forwardRef<any, Props>(
  ({ disabledLink = false, sx }, ref) => {
    const theme = useTheme();
    const { isCollapse } = useCollapseDrawer();
    const { user } = useAuth();
    const { workspaces } = useSelector((state) => state.auth);
    const { branding } = useSelector((state) => state.tenantBranding);

    const hostname = typeof window !== 'undefined' ? window.location.hostname : '';

    const resolvedWorkspace = getTenantWorkspaceBranding(workspaces, hostname);
    const brandColor = normalizeTenantBrandColor(
      resolvedWorkspace?.color ?? resolvedWorkspace?.primaryColor ?? branding?.color ?? branding?.primaryColor
    );
    const brandTextColor = getTenantBrandContrastText(brandColor);
    const logoSrc = resolvedWorkspace?.logoUrl || branding?.logoUrl || '';
    const fallbackLabel = resolvedWorkspace?.name?.trim()?.charAt(0)?.toUpperCase() || 'T';

    const userRole = user?.role?.toUpperCase().trim() ?? '';
    const path =
      userRole === 'TENANT_ADMIN' ? '/tenant-admin/dashboard' : '/client/dashboard';

    const logo = logoSrc ? (
      <Box
        ref={ref}
        component="img"
        src={logoSrc}
        alt={resolvedWorkspace?.name || 'Tenant logo'}
        sx={{
          height: isCollapse ? 40 : 36,
          width: 'auto',
          maxWidth: isCollapse ? 40 : 156,
          objectFit: 'contain',
          cursor: 'pointer',
          ...sx,
        }}
      />
    ) : (
      <Box
        ref={ref}
        sx={{
          height: isCollapse ? 40 : 36,
          minWidth: isCollapse ? 40 : 36,
          borderRadius: 1,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: brandColor || theme.palette.primary.main,
          color: brandTextColor,
          fontWeight: 700,
          fontSize: 15,
          cursor: 'pointer',
          ...sx,
        }}
      >
        {fallbackLabel}
      </Box>
    );

    if (disabledLink) {
      return <>{logo}</>;
    }

    return <NextLink href={path}>{logo}</NextLink>;
  }
);

export default LogoTenant;
