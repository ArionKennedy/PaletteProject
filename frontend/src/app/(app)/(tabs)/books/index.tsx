import MediaSearchPage from '@/components/media-search-page';

export default function BooksSearchPage() {
  return (
    <MediaSearchPage
      mediaType="book"
      title="Books"
      getDetailHref={(id) => ({ pathname: '/books/[id]', params: { id } })}
    />
  );
}
