import MediaSearchPage from '@/components/media-search-page';

export default function MoviesSearchPage() {
  return <MediaSearchPage mediaType="movie" title="Movies" getDetailHref={(id) => ({ pathname: '/movies/[id]', params: { id } })} />;
}