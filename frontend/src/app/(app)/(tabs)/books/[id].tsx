import { useLocalSearchParams } from 'expo-router';

import { MediaDetails } from '@/components/media-details';

export default function DetailsScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  return <MediaDetails key={id} id={id} type="book" />;
}
