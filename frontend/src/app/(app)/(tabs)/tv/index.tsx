import MediaSearchPage from '@/components/media-search-page';

export default function TvSearchPage() {
  return <MediaSearchPage mediaType="tv" title="TV Shows" getDetailHref={(id) => ({ pathname: '/tv/[id]', params: { id } })} />;
}