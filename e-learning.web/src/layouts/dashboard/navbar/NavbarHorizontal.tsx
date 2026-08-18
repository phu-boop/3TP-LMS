import { memo } from 'react';
import { useRouter } from 'next/router';
// @mui
import { styled } from '@mui/material/styles';
import { Container, AppBar } from '@mui/material';
// config
import { HEADER } from '../../../config';
// components
import { NavSectionHorizontal } from '../../../components/nav-section';
//
import dashboardNavConfig from './NavConfig';
import adminNavConfig from './AdminNavConfig';
import tenantNavConfig from './NavConfigTenant';
import { NavSectionProps } from '../../../components/nav-section/type';
import { useSelector } from '../../../redux/store';
import {
  getTenantBrandContrastText,
  getTenantWorkspaceBranding,
  normalizeTenantBrandColor,
} from '@/components/LogoTenant';

// ----------------------------------------------------------------------

const RootStyle = styled(AppBar)(({ theme }) => ({
  transition: theme.transitions.create('top', {
    easing: theme.transitions.easing.easeInOut,
    duration: theme.transitions.duration.shorter,
  }),
  width: '100%',
  position: 'fixed',
  zIndex: theme.zIndex.appBar,
  padding: theme.spacing(1, 0),
  boxShadow: theme.customShadows.z8,
  top: HEADER.DASHBOARD_DESKTOP_OFFSET_HEIGHT,
  backgroundColor: theme.palette.background.default,
}));

// ----------------------------------------------------------------------

function NavbarHorizontal() {
  const host = window.location.hostname;
  const { pathname } = useRouter();
  const { workspaces } = useSelector((state) => state.auth);
  const { resolve } = useSelector((state) => state.tenantBranding);
  const branding = resolve?.branding;

  const isTenantAdminPortal = pathname.startsWith('/tenant-admin');
  const isClientPortal = pathname.startsWith('/client');
  const isShowTenantBranding = isTenantAdminPortal || isClientPortal;

  const resolvedTenantBranding = getTenantWorkspaceBranding(workspaces, host);
  const tenantBrandColor = normalizeTenantBrandColor(
    resolvedTenantBranding?.color ?? resolvedTenantBranding?.primaryColor ?? branding?.color ?? branding?.primaryColor
  );
  const tenantBrandTextColor = getTenantBrandContrastText(tenantBrandColor);
  const tenantBrandHoverColor = tenantBrandColor ? `${tenantBrandColor}CC` : '#065f46';

  let navConfig: NavSectionProps['navConfig'] = dashboardNavConfig;
  if (pathname.startsWith('/admin')) {
    navConfig = adminNavConfig;
  } else if (isTenantAdminPortal) {
    navConfig = tenantNavConfig;
  }

  return (
    <RootStyle
      sx={{
        ...(isShowTenantBranding && {
          bgcolor: tenantBrandColor || '#064e3b',
          '& .MuiTypography-root': { color: tenantBrandTextColor },
          '& .MuiListItemIcon-root': { color: tenantBrandTextColor },
          '& .MuiListItemButton-root': {
            '&.active': {
              bgcolor: `${tenantBrandColor || '#059669'} !important`,
              color: `${tenantBrandTextColor} !important`,
              '& .MuiListItemIcon-root': { color: `${tenantBrandTextColor} !important` },
              '& .MuiTypography-root': { color: `${tenantBrandTextColor} !important` },
            },
            '&:hover': {
              bgcolor: `${tenantBrandHoverColor} !important`,
            },
          },
        }),
      }}
    >
      <Container maxWidth={false}>
        <NavSectionHorizontal navConfig={navConfig} />
      </Container>
    </RootStyle>
  );
}

export default memo(NavbarHorizontal);
