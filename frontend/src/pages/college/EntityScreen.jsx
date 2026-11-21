import ResourcePage from '../../components/ResourcePage';
import { canWrite, } from '../../config/navigation';
import { resourceConfigs, resourceLookups } from '../../config/resources';
import { useLookups } from '../../hooks/useLookups';
import { useCollege } from './CollegeContext';

/** Renders any configured entity (students, faculty, …) through the generic ResourcePage. */
export default function EntityScreen({ name }) {
  const { api, user } = useCollege();
  const lookups = useLookups(api, resourceLookups[name]);
  const config = resourceConfigs[name](lookups);
  return (
    <ResourcePage key={name} {...config} resource={api.resource(name)}
      canWrite={canWrite[name]?.includes(user.role)} />
  );
}
