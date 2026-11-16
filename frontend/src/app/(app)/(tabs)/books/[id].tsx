import { useEffect, useState } from 'react';
import { useLocalSearchParams } from 'expo-router';

import type { BookLookupResult } from '@/types/bookLookupResult';
import { bookLookup } from '@/services/lookup';
import MediaDetailsPage from '@/components/media-details-page';

export default function BookDetailsPage() {
  const { id } = useLocalSearchParams<{ id: string }>();
  return <BookDetails key={id} id={id} />;
}

function BookDetails({ id }: { id: string }) {
  const [book, setBook] = useState<BookLookupResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    bookLookup(id, controller.signal)
      .then(setBook)
      .catch((e: Error) => {
        if (e.name !== 'AbortError') setError(e.message);
      });
    return () => controller.abort();
  }, [id]);

  const metaText = book
    ? [
        book.releaseDate ? `Published: ${book.releaseDate.slice(0, 4)}` : null,
        book.isbn ? `ISBN: ${book.isbn}` : null,
      ]
        .filter(Boolean)
        .join(' · ')
    : '';

  return <MediaDetailsPage media={book} error={error} metaText={metaText} errorTitle="Book" />;
}
