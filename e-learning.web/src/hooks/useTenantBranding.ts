import { useEffect } from 'react';
import { useDispatch } from '../redux/store';
import { setBranding } from '../redux/slices/tenantBranding';
import RequestFactory from '../requests/RequestFactory';

export default function useTenantBranding() {
  const dispatch = useDispatch();

  useEffect(() => {
    const fetchBranding = async () => {
      try {
        const domain = typeof window !== 'undefined' ? window.location.host : undefined;
        const identityRequest = RequestFactory.getRequest('IdentityRequest');
        const res = await identityRequest.resolveBranding(domain);
        if (res.data) {
          dispatch(setBranding(res.data));
        }
      } catch (err) {
        console.error('Failed to fetch branding:', err);
      }
    };
    fetchBranding();
  }, [dispatch]);
}