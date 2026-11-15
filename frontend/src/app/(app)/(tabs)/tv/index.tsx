import MediaSearchPage from '@/components/media-search-page';

export default function TvSearchPage() {
  return <MediaSearchPage mediaType="tv" title="TV Shows" detailPathname="/tv/[id]" />;
}