import MediaSearchPage from '@/components/media-search-page';

export default function GamesSearchPage() {
  return (
    <MediaSearchPage
      mediaType="game"
      title="Games"
      getDetailHref={(id) => ({ pathname: '/games/[id]', params: { id } })}
    />
  );
}
