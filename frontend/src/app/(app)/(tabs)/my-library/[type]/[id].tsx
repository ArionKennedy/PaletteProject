import { Redirect, useLocalSearchParams } from 'expo-router';

import { MediaDetails } from '@/components/media-details';
import { LIBRARY_CATEGORIES } from '@/types/library';

export default function LibraryDetailsScreen() {
  const { type, id } = useLocalSearchParams<{ type: string; id: string }>();
  const category = LIBRARY_CATEGORIES.find(item => item.type === type);
  if (!category) return <Redirect href="/my-library" />;

  return <MediaDetails key={`${category.type}:${id}`} type={category.type} id={id}
    backHref={{ pathname: '/my-library/[type]', params: { type: category.type } }} />;
}
